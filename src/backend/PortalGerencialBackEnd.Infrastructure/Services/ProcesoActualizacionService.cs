using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Dtos.Sistema;
using PortalGerencialBackEnd.Core.Entities;
using PortalGerencialBackEnd.Core.Entities.Calculo;
using PortalGerencialBackEnd.Core.Entities.Sap;
using PortalGerencialBackEnd.Core.Enums;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    /// <summary>
    /// Una corrida del job: SAP → staging → resultados calculados. Cada paso queda en
    /// LOG_EXTRACCION_PGPM. Si un paso falla la corrida se detiene: los resultados que ya estaban
    /// calculados no se tocan y el portal sigue mostrándolos (sección 10).
    /// </summary>
    public class ProcesoActualizacionService : IProcesoActualizacionService
    {
        // Oracle admite hasta 1000 elementos en un IN (...).
        private const int TamanoLote = 900;

        private readonly PortalGerencialDbContext _db;
        private readonly ISapExtractionClient _sap;
        private readonly IStructureSource _estructura;
        private readonly ISistemaService _sistema;
        private readonly TimeProvider _time;
        private readonly ILogger<ProcesoActualizacionService> _logger;
        private readonly string _modo;

        public ProcesoActualizacionService(
            PortalGerencialDbContext db,
            ISapExtractionClient sap,
            IStructureSource estructura,
            ISistemaService sistema,
            TimeProvider time,
            IConfiguration configuration,
            ILogger<ProcesoActualizacionService> logger)
        {
            _db = db;
            _sap = sap;
            _estructura = estructura;
            _sistema = sistema;
            _time = time;
            _logger = logger;
            _modo = (configuration.GetValue<DataSourceMode?>("DataSourceMode") ?? DataSourceMode.Local).ToString().ToUpperInvariant();
        }

        private DateTime Ahora => _time.GetLocalNow().DateTime;

        public async Task<CorridaDto> EjecutarAsync(string origen, CancellationToken ct = default)
        {
            var corridaId = Guid.NewGuid().ToString();
            var origenLog = $"{origen}:{_modo}";
            var parametros = await ParametrosPortal.CargarAsync(_db);
            var cargaInicial = parametros.Fecha(ClavesParametro.FechaCargaInicial, new DateTime(Ahora.Year, 1, 1));

            _logger.LogInformation("Corrida {CorridaId} ({Origen}) iniciada.", corridaId, origenLog);

            var ok =
                await PasoAsync(corridaId, origenLog, FuentesLog.SapEquipos, null, _ => ExtraerEquiposAsync(ct))
                && await PasoAsync(corridaId, origenLog, FuentesLog.SapOrdenes, await DesdeAsync(FuentesLog.SapOrdenes, cargaInicial), d => ExtraerOrdenesAsync(d, ct))
                && await PasoAsync(corridaId, origenLog, FuentesLog.SapOperaciones, await DesdeAsync(FuentesLog.SapOperaciones, cargaInicial), d => ExtraerOperacionesAsync(d, ct))
                && await PasoAsync(corridaId, origenLog, FuentesLog.SapAvisos, await DesdeAsync(FuentesLog.SapAvisos, cargaInicial), d => ExtraerAvisosAsync(d, ct))
                && await PasoAsync(corridaId, origenLog, FuentesLog.SapHistorial, await DesdeAsync(FuentesLog.SapHistorial, cargaInicial), d => ExtraerHistorialAsync(d, ct));

            if (ok)
            {
                var contexto = await CargarContextoAsync(parametros);
                ok = await PasoAsync(corridaId, origenLog, FuentesLog.CalcSinClasificar, null, _ => CalcularSinClasificarAsync(contexto, corridaId))
                     && await PasoAsync(corridaId, origenLog, FuentesLog.CalcDisponibilidad, contexto.DesdeDisponibilidad, _ => CalcularDisponibilidadAsync(contexto, corridaId))
                     && await PasoAsync(corridaId, origenLog, FuentesLog.CalcAvance, null, _ => CalcularAvanceAsync(contexto, corridaId));
            }

            _logger.LogInformation("Corrida {CorridaId} terminada: {Resultado}.", corridaId, ok ? "OK" : "ERROR");
            return (await _sistema.GetCorridaAsync(corridaId))!;
        }

        // ---------------------------------------------------------------- bitácora

        private async Task<bool> PasoAsync(string corridaId, string origen, string fuente, DateTime? desde, Func<DateTime, Task<int>> accion)
        {
            var log = new LogExtraccion
            {
                CorridaId = corridaId,
                Fuente = fuente,
                Inicio = Ahora,
                Estado = EstadosLog.EnCurso,
                FechaCorteDesde = desde,
                Origen = origen
            };
            _db.LogExtracciones.Add(log);
            await _db.SaveChangesAsync();

            try
            {
                log.RegistrosProcesados = await accion(desde ?? DateTime.MinValue);
                log.Estado = EstadosLog.Ok;
                log.Fin = Ahora;
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Corrida {CorridaId}: falló el paso {Fuente}.", corridaId, fuente);
                _db.ChangeTracker.Clear(); // descarta lo que el paso dejó a medias
                log.Estado = EstadosLog.Error;
                log.Fin = Ahora;
                log.ErrorMensaje = ex.GetBaseException() == ex ? ex.Message : $"{ex.Message} → {ex.GetBaseException().Message}";
                _db.LogExtracciones.Update(log);
                await _db.SaveChangesAsync();
                return false;
            }
        }

        /// <summary>
        /// Delta: desde el día de la última extracción OK de esa fuente, menos un día de solape. La
        /// primera vez, la fecha de carga inicial (salida en vivo de SAP).
        /// </summary>
        private async Task<DateTime> DesdeAsync(string fuente, DateTime cargaInicial)
        {
            var ultimaOk = await _db.LogExtracciones
                .Where(l => l.Fuente == fuente && l.Estado == EstadosLog.Ok)
                .MaxAsync(l => (DateTime?)l.Inicio);
            return ultimaOk?.Date.AddDays(-1) ?? cargaInicial;
        }

        // ---------------------------------------------------------------- extracción SAP → staging

        private async Task<int> ExtraerEquiposAsync(CancellationToken ct)
        {
            var equipos = await _sap.GetEquiposAsync(ct);
            var existentes = await _db.StgSapEquipos.ToDictionaryAsync(e => e.EquipoSapId, ct);
            var ahora = Ahora;

            foreach (var dto in equipos)
            {
                if (!existentes.TryGetValue(dto.EquipoSapId, out var e))
                {
                    e = new StgSapEquipo { EquipoSapId = dto.EquipoSapId };
                    _db.StgSapEquipos.Add(e);
                    existentes[dto.EquipoSapId] = e;
                }
                e.CodigoLegado = string.IsNullOrWhiteSpace(dto.CodigoLegado) ? null : dto.CodigoLegado.Trim();
                e.Descripcion = dto.Descripcion;
                e.PuestoTrabajo = dto.PuestoTrabajo;
                e.Inactivo = dto.Inactivo;
                e.ActualizadoEn = ahora;
            }

            await _db.SaveChangesAsync(ct);
            return equipos.Count;
        }

        private async Task<int> ExtraerOrdenesAsync(DateTime desde, CancellationToken ct)
        {
            var ordenes = await _sap.GetOrdenesModificadasDesdeAsync(desde, ct);
            var ahora = Ahora;

            // Una orden puede apuntar a un equipo que no vino en el maestro: se registra vacío para
            // no romper la FK y, sin código legado, aparece en "equipos sin clasificar".
            var equiposConocidos = (await _db.StgSapEquipos.Select(e => e.EquipoSapId).ToListAsync(ct)).ToHashSet();
            foreach (var id in ordenes.Select(o => o.EquipoSapId).OfType<string>().Distinct().Where(id => !equiposConocidos.Contains(id)))
                _db.StgSapEquipos.Add(new StgSapEquipo { EquipoSapId = id, ActualizadoEn = ahora });

            foreach (var lote in ordenes.Chunk(TamanoLote))
            {
                var ids = lote.Select(o => o.OrdenSapId).ToList();
                var existentes = await _db.StgSapOrdenes.Where(o => ids.Contains(o.OrdenSapId)).ToDictionaryAsync(o => o.OrdenSapId, ct);
                foreach (var dto in lote)
                {
                    if (!existentes.TryGetValue(dto.OrdenSapId, out var o))
                    {
                        o = new StgSapOrden { OrdenSapId = dto.OrdenSapId };
                        _db.StgSapOrdenes.Add(o);
                    }
                    o.ClaseOrden = dto.ClaseOrden;
                    o.ClaseActividad = dto.ClaseActividad;
                    o.EquipoSapId = dto.EquipoSapId;
                    o.Descripcion = dto.Descripcion;
                    o.PuestoTrabajo = dto.PuestoTrabajo;
                    o.AvisoSapId = dto.AvisoSapId;
                    o.EstadoOrden = dto.EstadoOrden;
                    o.StatusSistema = dto.StatusSistema;
                    o.StatusUsuario = dto.StatusUsuario;
                    o.FechaCreacion = dto.FechaCreacion;
                    o.FechaInicioPlan = dto.FechaInicioPlan;
                    o.FechaFinPlan = dto.FechaFinPlan;
                    o.FechaInicioReal = dto.FechaInicioReal;
                    o.FechaFinReal = dto.FechaFinReal;
                    o.FechaCierre = dto.FechaCierre;
                    o.ActualizadoEn = ahora;
                }
            }

            await _db.SaveChangesAsync(ct);
            return ordenes.Count;
        }

        private async Task<int> ExtraerOperacionesAsync(DateTime desde, CancellationToken ct)
        {
            var operaciones = await _sap.GetOperacionesModificadasDesdeAsync(desde, ct);
            var ahora = Ahora;
            var procesadas = 0;

            foreach (var lote in operaciones.GroupBy(o => o.OrdenSapId).Chunk(TamanoLote))
            {
                var ids = lote.Select(g => g.Key).ToList();
                var ordenesExistentes = (await _db.StgSapOrdenes.Where(o => ids.Contains(o.OrdenSapId)).Select(o => o.OrdenSapId).ToListAsync(ct)).ToHashSet();
                var existentes = await _db.StgSapOperaciones.Where(o => ids.Contains(o.OrdenSapId)).ToDictionaryAsync(o => (o.OrdenSapId, o.Operacion), ct);

                foreach (var dto in lote.SelectMany(g => g))
                {
                    // De una orden que aún no llegó al staging: entra en la próxima corrida (solape de un día).
                    if (!ordenesExistentes.Contains(dto.OrdenSapId))
                        continue;
                    if (!existentes.TryGetValue((dto.OrdenSapId, dto.Operacion), out var o))
                    {
                        o = new StgSapOperacion { OrdenSapId = dto.OrdenSapId, Operacion = dto.Operacion };
                        _db.StgSapOperaciones.Add(o);
                        existentes[(dto.OrdenSapId, dto.Operacion)] = o;
                    }
                    o.Texto = dto.Texto;
                    o.PuestoTrabajo = dto.PuestoTrabajo;
                    o.Sistema = dto.Sistema;
                    o.Subsistema = dto.Subsistema;
                    o.Componente = dto.Componente;
                    o.TrabajoPlanHh = dto.TrabajoPlanHh;
                    o.TrabajoRealHh = dto.TrabajoRealHh;
                    o.FechaInicioPlan = dto.FechaInicioPlan;
                    o.FechaFinPlan = dto.FechaFinPlan;
                    o.FechaPrimeraConf = dto.FechaPrimeraConf;
                    o.FechaUltimaConf = dto.FechaUltimaConf;
                    o.ConfFinal = dto.ConfFinal;
                    o.Programada = dto.Programada;
                    o.ActualizadoEn = ahora;
                    procesadas++;
                }
            }

            await _db.SaveChangesAsync(ct);
            return procesadas;
        }

        private async Task<int> ExtraerAvisosAsync(DateTime desde, CancellationToken ct)
        {
            var avisos = await _sap.GetAvisosModificadosDesdeAsync(desde, ct);
            var ahora = Ahora;

            foreach (var lote in avisos.Chunk(TamanoLote))
            {
                var ids = lote.Select(a => a.AvisoSapId).ToList();
                var existentes = await _db.StgSapAvisos.Where(a => ids.Contains(a.AvisoSapId)).ToDictionaryAsync(a => a.AvisoSapId, ct);
                foreach (var dto in lote)
                {
                    if (!existentes.TryGetValue(dto.AvisoSapId, out var a))
                    {
                        a = new StgSapAviso { AvisoSapId = dto.AvisoSapId };
                        _db.StgSapAvisos.Add(a);
                    }
                    a.ClaseAviso = dto.ClaseAviso;
                    a.Texto = dto.Texto;
                    a.EquipoSapId = dto.EquipoSapId;
                    a.OrdenSapId = dto.OrdenSapId;
                    a.FechaAviso = dto.FechaAviso;
                    a.Parada = dto.Parada;
                    a.InicioAveria = dto.InicioAveria;
                    a.FinAveria = dto.FinAveria;
                    a.StatusSistema = dto.StatusSistema;
                    a.FechaCierre = dto.FechaCierre;
                    a.ActualizadoEn = ahora;
                }
            }

            await _db.SaveChangesAsync(ct);
            return avisos.Count;
        }

        private async Task<int> ExtraerHistorialAsync(DateTime desde, CancellationToken ct)
        {
            var historial = await _sap.GetHistorialEstadosDesdeAsync(desde, ct);
            var ahora = Ahora;

            foreach (var lote in historial.GroupBy(h => h.OrdenSapId).Chunk(TamanoLote))
            {
                var ids = lote.Select(g => g.Key).ToList();
                var existentes = await _db.StgSapHistorialEstados.Where(h => ids.Contains(h.OrdenSapId))
                    .ToDictionaryAsync(h => (h.OrdenSapId, h.EstadoCodigo, h.NumeroCambio), ct);
                foreach (var dto in lote.SelectMany(g => g))
                {
                    if (!existentes.TryGetValue((dto.OrdenSapId, dto.EstadoCodigo, dto.NumeroCambio), out var h))
                    {
                        h = new StgSapHistorialEstado { OrdenSapId = dto.OrdenSapId, EstadoCodigo = dto.EstadoCodigo, NumeroCambio = dto.NumeroCambio };
                        _db.StgSapHistorialEstados.Add(h);
                        existentes[(dto.OrdenSapId, dto.EstadoCodigo, dto.NumeroCambio)] = h;
                    }
                    h.EstadoTexto = dto.EstadoTexto;
                    h.FechaHoraCambio = dto.FechaHoraCambio;
                    h.Indicador = dto.Indicador;
                    h.ActualizadoEn = ahora;
                }
            }

            await _db.SaveChangesAsync(ct);
            return historial.Count;
        }

        // ---------------------------------------------------------------- cálculo

        private sealed class ContextoCalculo
        {
            public DateTime Ahora { get; init; }
            public DateTime DesdeDisponibilidad { get; init; }
            public ReglasVentana Reglas { get; init; } = default!;
            public HashSet<string> ClasesAvance { get; init; } = default!;
            public BasePlan BasePlan { get; init; }
            public MapeoEquipos Equipos { get; init; } = default!;
        }

        private async Task<ContextoCalculo> CargarContextoAsync(ParametrosPortal p)
        {
            var ahora = Ahora;
            return new ContextoCalculo
            {
                Ahora = ahora,
                DesdeDisponibilidad = ahora.Date.AddDays(-p.Entero(ClavesParametro.DiasRecalculoDisponibilidad, 120)),
                Reglas = ReglasPortal.Ventana(p),
                ClasesAvance = ReglasPortal.ClasesAvance(p),
                BasePlan = ReglasPortal.BasePlan(p),
                Equipos = await MapeoEquipos.CargarAsync(_db, _estructura)
            };
        }

        private async Task<int> CalcularSinClasificarAsync(ContextoCalculo c, string corridaId)
        {
            var ordenesPorEquipo = await _db.StgSapOrdenes.AsNoTracking()
                .Where(o => o.EquipoSapId != null)
                .GroupBy(o => o.EquipoSapId!)
                .Select(g => new { EquipoSapId = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.EquipoSapId, x => x.Cantidad);

            var ahora = Ahora;
            var sinClasificar = (await _db.StgSapEquipos.AsNoTracking().ToListAsync())
                .Select(e => new
                {
                    Equipo = e,
                    Motivo = e.CodigoLegado == null ? "SIN_CODIGO_LEGADO"
                        : c.Equipos.EquiposOracle.ContainsKey(e.CodigoLegado) ? null : "CODIGO_NO_EXISTE_EN_ORACLE"
                })
                .Where(x => x.Motivo != null)
                .Select(x => new CalcEquipoSinClasificar
                {
                    EquipoSapId = x.Equipo.EquipoSapId,
                    CodigoLegado = x.Equipo.CodigoLegado,
                    Descripcion = x.Equipo.Descripcion,
                    PuestoTrabajo = x.Equipo.PuestoTrabajo,
                    Motivo = x.Motivo!,
                    CantOrdenes = ordenesPorEquipo.GetValueOrDefault(x.Equipo.EquipoSapId),
                    CorridaId = corridaId,
                    DetectadoEn = ahora
                })
                .ToList();

            await ReemplazarAsync(async () =>
            {
                await _db.CalcEquiposSinClasificar.ExecuteDeleteAsync();
                _db.CalcEquiposSinClasificar.AddRange(sinClasificar);
            });
            return sinClasificar.Count;
        }

        /// <summary>
        /// Ventanas fuera de servicio de todas las órdenes que tocan [desde, ahora]: las usa el
        /// cálculo y también los drill-down de la pantalla (ReporteDisponibilidadService).
        /// </summary>
        internal static async Task<ILookup<string, VentanaFueraServicio>> VentanasAsync(
            PortalGerencialDbContext db, Dictionary<string, string> legadoPorEquipoSap, ReglasVentana reglas, DateTime desde, DateTime ahora)
        {
            var ordenes = await db.StgSapOrdenes.AsNoTracking()
                .Where(o => o.EquipoSapId != null && o.FechaCreacion <= ahora && (o.FechaCierre == null || o.FechaCierre >= desde))
                .Select(o => new
                {
                    o.OrdenSapId, o.EquipoSapId, o.ClaseOrden, o.EstadoOrden, o.FechaCreacion,
                    o.FechaInicioReal, o.FechaFinReal, o.FechaCierre, o.AvisoSapId
                })
                .ToListAsync();

            var avisoIds = ordenes.Select(o => o.AvisoSapId).OfType<string>().Distinct().ToList();
            var avisos = new Dictionary<string, AvisoDisponibilidad>();
            foreach (var lote in avisoIds.Chunk(TamanoLote))
            {
                foreach (var a in await db.StgSapAvisos.AsNoTracking().Where(a => lote.Contains(a.AvisoSapId)).ToListAsync())
                    avisos[a.AvisoSapId] = new AvisoDisponibilidad(a.Parada, a.InicioAveria, a.FinAveria);
            }

            return ordenes
                .Where(o => legadoPorEquipoSap.ContainsKey(o.EquipoSapId!))
                .Select(o => CalculadoraDisponibilidad.Ventana(new OrdenDisponibilidad(
                    o.OrdenSapId, legadoPorEquipoSap[o.EquipoSapId!], o.ClaseOrden, o.EstadoOrden, o.FechaCreacion,
                    o.FechaInicioReal, o.FechaFinReal, o.FechaCierre,
                    o.AvisoSapId != null && avisos.TryGetValue(o.AvisoSapId, out var av) ? av : null), reglas, ahora))
                .OfType<VentanaFueraServicio>()
                .ToLookup(v => v.CodigoLegado, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<int> CalcularDisponibilidadAsync(ContextoCalculo c, string corridaId)
        {
            var hoy = c.Ahora.Date;
            var ventanas = await VentanasAsync(_db, c.Equipos.LegadoPorEquipoSap, c.Reglas, c.DesdeDisponibilidad.AddDays(-7), c.Ahora);
            var ahora = Ahora;
            var diariaTotal = new List<CalcDisponibilidadDiaria>();
            var semanalTotal = new List<CalcDisponibilidadSemanal>();
            var borrar = new List<(int ReporteId, DateTime Desde, List<int> Semanas)>();

            foreach (var config in await _estructura.GetConfigsDisponibilidadAsync())
            {
                // Semanas del calendario del reporte que caen en la ventana de recálculo.
                var semanas = config.Semanas.Where(s => s.FechaFin >= c.DesdeDisponibilidad && s.FechaInicio <= hoy).ToList();
                if (semanas.Count == 0)
                    continue;
                borrar.Add((config.ReporteId, semanas[0].FechaInicio, semanas.Select(s => s.NoSemana).ToList()));

                foreach (var asignacion in config.Asignaciones)
                {
                    // Cuentan los equipos activos en Oracle y en SAP.
                    var equipos = asignacion.Equipos.Where(c.Equipos.CuentaParaDisponibilidad).ToList();

                    foreach (var semana in semanas)
                    {
                        var dias = new List<CalcDisponibilidadDiaria>();
                        for (var dia = semana.FechaInicio; dia <= semana.FechaFin && dia <= hoy; dia = dia.AddDays(1))
                        {
                            var d = CalculadoraDisponibilidad.CalcularDia(config.ReporteId, asignacion, equipos, config.HorasActivas, ventanas, dia);
                            d.CorridaId = corridaId;
                            d.GeneradoEn = ahora;
                            dias.Add(d);
                        }
                        if (dias.Count == 0)
                            continue;
                        diariaTotal.AddRange(dias);
                        var s = CalculadoraDisponibilidad.AgregarSemana(config.ReporteId, asignacion.AsignacionId, semana, dias);
                        s.CorridaId = corridaId;
                        s.GeneradoEn = ahora;
                        semanalTotal.Add(s);
                    }
                }
            }

            await ReemplazarAsync(async () =>
            {
                foreach (var (reporteId, desde, numeros) in borrar)
                {
                    await _db.CalcDisponibilidadDiaria.Where(d => d.ReporteId == reporteId && d.Fecha >= desde).ExecuteDeleteAsync();
                    await _db.CalcDisponibilidadSemanal.Where(s => s.ReporteId == reporteId && numeros.Contains(s.NoSemana)).ExecuteDeleteAsync();
                }
                _db.CalcDisponibilidadDiaria.AddRange(diariaTotal);
                _db.CalcDisponibilidadSemanal.AddRange(semanalTotal);
            });
            return diariaTotal.Count;
        }

        private async Task<int> CalcularAvanceAsync(ContextoCalculo c, string corridaId)
        {
            var clases = c.ClasesAvance.ToList();
            var ordenes = await _db.StgSapOrdenes.AsNoTracking()
                .Where(o => o.EquipoSapId != null && clases.Contains(o.ClaseOrden))
                .Include(o => o.Operaciones)
                .ToListAsync();

            var ahora = Ahora;
            var resultado = new List<CalcAvanceSemanal>();
            foreach (var reporte in await _estructura.GetConfigsAvanceAsync())
            {
                foreach (var familia in reporte.Familias)
                {
                    var equiposPrograma = ReglasPortal.EquiposDelPrograma(familia, c.Equipos);
                    var delPrograma = ordenes.Where(o => ReglasPortal.EsDelPrograma(o, c.Equipos, reporte, equiposPrograma, c.Reglas)).ToList();
                    var ordenesAvance = delPrograma.Select(o => new OrdenAvance(o.OrdenSapId, ReglasPortal.CierreSiCerrada(o, c.Reglas))).ToList();
                    var actividades = delPrograma.SelectMany(o => o.Operaciones)
                        .Select(op => new ActividadAvance(ReglasPortal.DiasActividad(op), ReglasPortal.Terminada(op), op.Programada))
                        .ToList();

                    var filas = CalculadoraAvance.Calcular(reporte.ReporteId, familia.FamiliaCodigo, familia.Semanas, c.BasePlan, ordenesAvance, actividades);
                    foreach (var f in filas)
                    {
                        f.CorridaId = corridaId;
                        f.GeneradoEn = ahora;
                    }
                    resultado.AddRange(filas);
                }
            }

            await ReemplazarAsync(async () =>
            {
                await _db.CalcAvanceSemanal.ExecuteDeleteAsync();
                _db.CalcAvanceSemanal.AddRange(resultado);
            });
            return resultado.Count;
        }

        /// <summary>Borra y vuelve a insertar en una sola transacción: o queda todo nuevo o todo como estaba.</summary>
        private async Task ReemplazarAsync(Func<Task> borrarYAgregar)
        {
            await using var transaccion = await _db.Database.BeginTransactionAsync();
            await borrarYAgregar();
            await _db.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
    }
}
