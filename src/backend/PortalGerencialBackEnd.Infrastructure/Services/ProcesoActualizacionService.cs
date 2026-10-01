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
    /// Una corrida del job (sección 6 y 12): SAP → staging → resultados calculados. Cada paso queda
    /// en LOG_EXTRACCION_PGPM. Si un paso falla la corrida se detiene: los resultados que ya estaban
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
            var hoy = Ahora.Date;
            var parametros = await ParametrosPortal.CargarAsync(_db);
            var cargaInicial = parametros.Fecha(ClavesParametro.FechaCargaInicial, new DateTime(hoy.Year, 1, 1));

            _logger.LogInformation("Corrida {CorridaId} ({Origen}) iniciada.", corridaId, origenLog);

            var ok =
                await PasoAsync(corridaId, origenLog, FuentesLog.SapEquipos, null, () => ExtraerEquiposAsync(ct))
                && await PasoAsync(corridaId, origenLog, FuentesLog.SapOrdenes, await DesdeAsync(FuentesLog.SapOrdenes, cargaInicial),
                    d => ExtraerOrdenesAsync(d, ct))
                && await PasoAsync(corridaId, origenLog, FuentesLog.SapHistorial, await DesdeAsync(FuentesLog.SapHistorial, cargaInicial),
                    d => ExtraerHistorialAsync(d, ct))
                && await PasoAsync(corridaId, origenLog, FuentesLog.SapHoras, await DesdeAsync(FuentesLog.SapHoras, cargaInicial),
                    d => ExtraerHorasAsync(d, ct));

            if (ok)
            {
                var contexto = await CargarContextoCalculoAsync(parametros, hoy);
                ok = await PasoAsync(corridaId, origenLog, FuentesLog.CalcSinClasificar, null, () => CalcularSinClasificarAsync(contexto, corridaId))
                     && await PasoAsync(corridaId, origenLog, FuentesLog.CalcDisponibilidad, contexto.DesdeDisponibilidad,
                         _ => CalcularDisponibilidadAsync(contexto, corridaId))
                     && await PasoAsync(corridaId, origenLog, FuentesLog.CalcAvance, null, () => CalcularAvanceAsync(contexto, corridaId));
            }

            _logger.LogInformation("Corrida {CorridaId} terminada: {Resultado}.", corridaId, ok ? "OK" : "ERROR");
            return (await _sistema.GetCorridaAsync(corridaId))!;
        }

        // ---------------------------------------------------------------- bitácora

        private Task<bool> PasoAsync(string corridaId, string origen, string fuente, DateTime? desde, Func<Task<int>> accion) =>
            PasoAsync(corridaId, origen, fuente, desde, _ => accion());

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

                // Descarta lo que el paso dejó a medias en el contexto antes de grabar el error.
                _db.ChangeTracker.Clear();
                log.Estado = EstadosLog.Error;
                log.Fin = Ahora;
                log.ErrorMensaje = ex.GetBaseException() == ex ? ex.Message : $"{ex.Message} → {ex.GetBaseException().Message}";
                _db.LogExtracciones.Update(log);
                await _db.SaveChangesAsync();
                return false;
            }
        }

        /// <summary>
        /// Delta: desde el día de la última extracción OK de esa fuente, menos un día de solape
        /// (lo que cambió en SAP durante la corrida anterior no se pierde). La primera vez, la fecha
        /// de carga inicial.
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
                e.ActualizadoEn = ahora;
            }

            await _db.SaveChangesAsync(ct);
            return equipos.Count;
        }

        private async Task<int> ExtraerOrdenesAsync(DateTime desde, CancellationToken ct)
        {
            var ordenes = await _sap.GetOrdenesModificadasDesdeAsync(desde, ct);
            var ahora = Ahora;

            // Una orden puede apuntar a un equipo que no vino en el maestro (p. ej. creado después
            // de la extracción de equipos): se registra vacío para no romper la FK, y como no tiene
            // código legado aparece en "equipos sin clasificar".
            var equiposConocidos = (await _db.StgSapEquipos.Select(e => e.EquipoSapId).ToListAsync(ct)).ToHashSet();
            foreach (var equipoId in ordenes.Select(o => o.EquipoSapId).OfType<string>().Distinct().Where(id => !equiposConocidos.Contains(id)))
                _db.StgSapEquipos.Add(new StgSapEquipo { EquipoSapId = equipoId, ActualizadoEn = ahora });

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
                    o.EquipoSapId = dto.EquipoSapId;
                    o.Descripcion = dto.Descripcion;
                    o.PuestoTrabajo = dto.PuestoTrabajo;
                    o.EstadoOrden = dto.EstadoOrden;
                    o.FechaCreacion = dto.FechaCreacion;
                    o.FechaInicioPlan = dto.FechaInicioPlan;
                    o.FechaFinPlan = dto.FechaFinPlan;
                    o.FechaInicioReal = dto.FechaInicioReal;
                    o.FechaFinReal = dto.FechaFinReal;
                    o.ActualizadoEn = ahora;
                }
            }

            await _db.SaveChangesAsync(ct);
            return ordenes.Count;
        }

        private async Task<int> ExtraerHistorialAsync(DateTime desde, CancellationToken ct)
        {
            var historial = await _sap.GetHistorialEstadosDesdeAsync(desde, ct);
            var ahora = Ahora;

            foreach (var lote in historial.GroupBy(h => h.OrdenSapId).Chunk(TamanoLote))
            {
                var ids = lote.Select(g => g.Key).ToList();
                var existentes = await _db.StgSapHistorialEstados
                    .Where(h => ids.Contains(h.OrdenSapId))
                    .ToDictionaryAsync(h => (h.OrdenSapId, h.EstadoCodigo, h.NumeroCambio), ct);

                foreach (var dto in lote.SelectMany(g => g))
                {
                    if (!existentes.TryGetValue((dto.OrdenSapId, dto.EstadoCodigo, dto.NumeroCambio), out var h))
                    {
                        h = new StgSapHistorialEstado
                        {
                            OrdenSapId = dto.OrdenSapId,
                            EstadoCodigo = dto.EstadoCodigo,
                            NumeroCambio = dto.NumeroCambio
                        };
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

        private async Task<int> ExtraerHorasAsync(DateTime desde, CancellationToken ct)
        {
            var horas = await _sap.GetHorasModificadasDesdeAsync(desde, ct);
            var ahora = Ahora;
            var procesadas = 0;

            foreach (var lote in horas.GroupBy(h => h.OrdenSapId).Chunk(TamanoLote))
            {
                var ids = lote.Select(g => g.Key).ToList();
                var ordenesExistentes = (await _db.StgSapOrdenes.Where(o => ids.Contains(o.OrdenSapId)).Select(o => o.OrdenSapId).ToListAsync(ct)).ToHashSet();
                var existentes = await _db.StgSapOperacionesHoras
                    .Where(h => ids.Contains(h.OrdenSapId))
                    .ToDictionaryAsync(h => (h.OrdenSapId, h.Operacion), ct);

                foreach (var dto in lote.SelectMany(g => g))
                {
                    // Horas de una orden que aún no llegó al staging: se toman en la próxima corrida
                    // (el delta tiene un día de solape).
                    if (!ordenesExistentes.Contains(dto.OrdenSapId))
                        continue;

                    if (!existentes.TryGetValue((dto.OrdenSapId, dto.Operacion), out var h))
                    {
                        h = new StgSapOperacionHoras { OrdenSapId = dto.OrdenSapId, Operacion = dto.Operacion };
                        _db.StgSapOperacionesHoras.Add(h);
                        existentes[(dto.OrdenSapId, dto.Operacion)] = h;
                    }
                    h.TrabajoPlanHh = dto.TrabajoPlanHh;
                    h.TrabajoRealHh = dto.TrabajoRealHh;
                    h.ActualizadoEn = ahora;
                    procesadas++;
                }
            }

            await _db.SaveChangesAsync(ct);
            return procesadas;
        }

        // ---------------------------------------------------------------- cálculo

        private sealed class ContextoCalculo
        {
            public DateTime Hoy { get; init; }
            public DateTime DesdeDisponibilidad { get; init; }
            public HashSet<string> EstadosFueraServicio { get; init; } = default!;
            public HashSet<string> EstadosCerrados { get; init; } = default!;
            public HashSet<string> ClasesAvance { get; init; } = default!;
            public FormulaAvanceReal Formula { get; init; }

            /// <summary>Equipo SAP → código legado (solo los que tienen uno).</summary>
            public Dictionary<string, string> LegadoPorEquipoSap { get; init; } = default!;

            /// <summary>Código legado → familia, según Oracle.</summary>
            public Dictionary<string, (string Familia, bool Activo)> EquiposOracle { get; init; } = default!;
        }

        private async Task<ContextoCalculo> CargarContextoCalculoAsync(ParametrosPortal p, DateTime hoy)
        {
            var dias = p.Entero(ClavesParametro.DiasRecalculoDisponibilidad, 120);
            var desde = hoy.AddDays(-dias);
            // Arranca en lunes para que la primera semana del agregado semanal quede completa.
            desde = desde.AddDays(-(((int)desde.DayOfWeek + 6) % 7));

            var equiposOracle = await _estructura.GetEquiposFamiliaAsync();
            var legados = await _db.StgSapEquipos.AsNoTracking()
                .Where(e => e.CodigoLegado != null)
                .Select(e => new { e.EquipoSapId, e.CodigoLegado })
                .ToListAsync();

            return new ContextoCalculo
            {
                Hoy = hoy,
                DesdeDisponibilidad = desde,
                EstadosFueraServicio = p.Lista(ClavesParametro.EstadosFueraServicio, "Liberado,En Proceso"),
                EstadosCerrados = p.Lista(ClavesParametro.EstadosCerrados, "Cerrado tecnicamente"),
                ClasesAvance = p.Lista(ClavesParametro.ClasesOrdenAvance, "ZC03"),
                Formula = p.Texto(ClavesParametro.FormulaAvanceReal, "HH_ORDENES_CERRADAS") == "ORDENES_CERRADAS"
                    ? FormulaAvanceReal.OrdenesCerradas
                    : FormulaAvanceReal.HhOrdenesCerradas,
                LegadoPorEquipoSap = legados.ToDictionary(e => e.EquipoSapId, e => e.CodigoLegado!),
                EquiposOracle = equiposOracle
                    .GroupBy(e => e.CodigoLegado, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => (g.First().FamiliaCodigo, g.First().Activo), StringComparer.OrdinalIgnoreCase)
            };
        }

        private async Task<int> CalcularSinClasificarAsync(ContextoCalculo c, string corridaId)
        {
            var ordenesPorEquipo = await _db.StgSapOrdenes.AsNoTracking()
                .Where(o => o.EquipoSapId != null)
                .GroupBy(o => o.EquipoSapId!)
                .Select(g => new { EquipoSapId = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.EquipoSapId, x => x.Cantidad);

            var equipos = await _db.StgSapEquipos.AsNoTracking().ToListAsync();
            var ahora = Ahora;
            var sinClasificar = equipos
                .Select(e => new
                {
                    Equipo = e,
                    Motivo = e.CodigoLegado == null
                        ? "SIN_CODIGO_LEGADO"
                        : c.EquiposOracle.ContainsKey(e.CodigoLegado) ? null : "CODIGO_NO_EXISTE_EN_ORACLE"
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

        private async Task<int> CalcularDisponibilidadAsync(ContextoCalculo c, string corridaId)
        {
            var requeridos = new Dictionary<(string, string), int>();
            foreach (var tipo in await _estructura.GetTiposReporteAsync())
            {
                foreach (var g in (await _estructura.GetEquiposRequeridosAsync(tipo.TipoReporte)).GroupBy(r => r.FamiliaCodigo))
                    requeridos[(tipo.TipoReporte, g.Key)] = g.Sum(r => r.CantRequerida);
            }

            var equiposActivosPorFamilia = c.EquiposOracle
                .Where(e => e.Value.Activo)
                .GroupBy(e => e.Value.Familia)
                .ToDictionary(g => g.Key, g => (IReadOnlyCollection<string>)g.Select(e => e.Key).ToList());

            var desde = c.DesdeDisponibilidad;
            var ordenes = (await _db.StgSapOrdenes.AsNoTracking()
                    .Where(o => o.EquipoSapId != null
                                && o.FechaInicioReal != null && o.FechaInicioReal <= c.Hoy
                                && (o.FechaFinReal == null || o.FechaFinReal >= desde))
                    .Select(o => new { o.EquipoSapId, o.EstadoOrden, o.FechaInicioReal, o.FechaFinReal })
                    .ToListAsync())
                .Where(o => c.LegadoPorEquipoSap.ContainsKey(o.EquipoSapId!))
                .Select(o => new OrdenDisponibilidad(c.LegadoPorEquipoSap[o.EquipoSapId!], o.EstadoOrden, o.FechaInicioReal, o.FechaFinReal))
                .ToList();

            var ahora = Ahora;
            var diaria = CalculadoraDisponibilidad.CalcularDiaria(
                equiposActivosPorFamilia, requeridos, ordenes, c.EstadosFueraServicio, desde, c.Hoy, c.Hoy);
            foreach (var d in diaria)
            {
                d.CorridaId = corridaId;
                d.GeneradoEn = ahora;
            }

            var semanal = CalculadoraDisponibilidad.AgregarSemanal(diaria);
            foreach (var s in semanal)
            {
                s.CorridaId = corridaId;
                s.GeneradoEn = ahora;
            }

            await ReemplazarAsync(async () =>
            {
                await _db.CalcDisponibilidadDiaria.Where(d => d.Fecha >= desde).ExecuteDeleteAsync();
                await _db.CalcDisponibilidadSemanal.Where(s => s.FechaInicioSemana >= desde).ExecuteDeleteAsync();
                _db.CalcDisponibilidadDiaria.AddRange(diaria);
                _db.CalcDisponibilidadSemanal.AddRange(semanal);
            });
            return diaria.Count;
        }

        private async Task<int> CalcularAvanceAsync(ContextoCalculo c, string corridaId)
        {
            var clases = c.ClasesAvance.ToList();
            var ordenes = await _db.StgSapOrdenes.AsNoTracking()
                .Where(o => o.EquipoSapId != null && clases.Contains(o.ClaseOrden))
                .Select(o => new
                {
                    o.EquipoSapId,
                    o.EstadoOrden,
                    o.FechaCreacion,
                    o.FechaInicioPlan,
                    o.FechaFinReal,
                    HhPlan = o.Operaciones.Sum(op => (decimal?)op.TrabajoPlanHh) ?? 0m,
                    HhReal = o.Operaciones.Sum(op => (decimal?)op.TrabajoRealHh) ?? 0m
                })
                .ToListAsync();

            // Orden → familia, vía código legado.
            var ordenesConFamilia = ordenes
                .Where(o => c.LegadoPorEquipoSap.TryGetValue(o.EquipoSapId!, out var legado) && c.EquiposOracle.ContainsKey(legado))
                .Select(o => new { Orden = o, Familia = c.EquiposOracle[c.LegadoPorEquipoSap[o.EquipoSapId!]].Familia })
                .ToList();

            var ahora = Ahora;
            var resultado = new List<CalcAvanceSemanal>();
            foreach (var programa in await _estructura.GetProgramasAsync())
            {
                var curva = (await _estructura.GetCurvaPlaneadaAsync(programa.AnioPrograma)).ToLookup(s => s.FamiliaCodigo);
                foreach (var familia in await _estructura.GetFamiliasAsync(programa.AnioPrograma))
                {
                    // Pertenece al programa la orden planificada (o creada) dentro de la ventana de la
                    // familia, con un mes de margen previo para las que se adelantan.
                    var ordenesFamilia = ordenesConFamilia
                        .Where(x => x.Familia == familia.FamiliaCodigo)
                        .Select(x => x.Orden)
                        .Where(o => (o.FechaInicioPlan ?? o.FechaCreacion) is { } f
                                    && f >= familia.FechaEstInicio.AddDays(-30) && f <= familia.FechaEstFin)
                        .Select(o => new OrdenAvance(o.FechaFinReal, c.EstadosCerrados.Contains(o.EstadoOrden), o.HhPlan, o.HhReal))
                        .ToList();

                    resultado.AddRange(CalculadoraAvance.Calcular(
                        programa.AnioPrograma, familia.FamiliaCodigo, curva[familia.FamiliaCodigo], ordenesFamilia, c.Formula, c.Hoy));
                }
            }

            foreach (var r in resultado)
            {
                r.CorridaId = corridaId;
                r.GeneradoEn = ahora;
            }

            await ReemplazarAsync(async () =>
            {
                await _db.CalcAvanceSemanal.ExecuteDeleteAsync();
                _db.CalcAvanceSemanal.AddRange(resultado);
            });
            return resultado.Count;
        }

        /// <summary>Borra y vuelve a insertar resultados en una sola transacción: o queda todo nuevo o todo como estaba.</summary>
        private async Task ReemplazarAsync(Func<Task> borrarYAgregar)
        {
            await using var transaccion = await _db.Database.BeginTransactionAsync();
            await borrarYAgregar();
            await _db.SaveChangesAsync();
            await transaccion.CommitAsync();
        }
    }
}
