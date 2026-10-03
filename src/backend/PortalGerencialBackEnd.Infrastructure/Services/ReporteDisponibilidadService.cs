using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Core.Dtos.Disponibilidad;
using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    /// <summary>
    /// Disponibilidad de Equipos, como el reporte de Oracle. Las semanas salen de los resultados ya
    /// calculados; las columnas actuales son la foto de la última hora medida antes de la última
    /// actualización (como "Última Actualización 17:00" en Oracle), calculada sobre el staging con
    /// las mismas reglas que el job. Nunca llama a SAP.
    /// </summary>
    public class ReporteDisponibilidadService : IReporteDisponibilidadService
    {
        private const int TamanoLote = 900;

        private readonly PortalGerencialDbContext _db;
        private readonly IStructureSource _estructura;
        private readonly ISistemaService _sistema;
        private readonly TimeProvider _time;

        public ReporteDisponibilidadService(PortalGerencialDbContext db, IStructureSource estructura, ISistemaService sistema, TimeProvider time)
        {
            _db = db;
            _estructura = estructura;
            _sistema = sistema;
            _time = time;
        }

        private sealed record Base(ConfigDisponibilidadDto Config, DateTime? Corte, MapeoEquipos Mapeo, ReglasVentana Reglas)
        {
            /// <summary>Corte, o ahora si todavía no hubo ninguna actualización.</summary>
            public DateTime Referencia(DateTime ahora) => Corte ?? ahora;

            public AsignacionDto Asignacion(int asignacionId) =>
                Config.Asignaciones.FirstOrDefault(a => a.AsignacionId == asignacionId)
                ?? throw new NotFoundException($"La asignación {asignacionId} no existe en el reporte {Config.ReporteId}.");

            public string Color(decimal pct) => Semaforo.Color(pct, Config.UmbralRojo, Config.UmbralNaranja);

            /// <summary>Última hora activa del reporte a la que ya se llegó en <paramref name="instante"/>.</summary>
            public DateTime InstanteFoto(DateTime instante)
            {
                var horas = Config.HorasActivas.Count == 0 ? Enumerable.Range(0, 24).ToList() : Config.HorasActivas;
                var hoy = horas.Where(h => h <= instante.Hour).ToList();
                return hoy.Count > 0 ? instante.Date.AddHours(hoy.Max()) : instante.Date.AddDays(-1).AddHours(horas.Max());
            }
        }

        private async Task<Base> CargarBaseAsync(int reporteId)
        {
            var config = await _estructura.GetConfigDisponibilidadAsync(reporteId)
                         ?? throw new NotFoundException($"No existe el reporte de disponibilidad {reporteId}.");
            var corte = (await _sistema.GetEstadoActualizacionAsync()).UltimaActualizacion;
            var parametros = await ParametrosPortal.CargarAsync(_db);
            return new Base(config, corte, await MapeoEquipos.CargarAsync(_db, _estructura), ReglasPortal.Ventana(parametros));
        }

        private DateTime Ahora => _time.GetLocalNow().DateTime;

        public async Task<ReporteDisponibilidadDto> GetReporteAsync(int reporteId)
        {
            var b = await CargarBaseAsync(reporteId);
            var referencia = b.Referencia(Ahora);
            var hoy = referencia.Date;

            var reporte = new ReporteDisponibilidadDto
            {
                ReporteId = b.Config.ReporteId,
                Descripcion = b.Config.Descripcion,
                UmbralRojo = b.Config.UmbralRojo,
                UmbralNaranja = b.Config.UmbralNaranja,
                FechaCorte = b.Corte == null ? null : b.InstanteFoto(b.Corte.Value),
                Semanas = b.Config.Semanas.Select(s => new SemanaColumnaDto
                {
                    NoSemana = s.NoSemana,
                    FechaInicio = s.FechaInicio,
                    FechaFin = s.FechaFin,
                    Momento = s.FechaFin < hoy ? "PASADA" : s.FechaInicio <= hoy ? "ACTUAL" : "FUTURA"
                }).ToList()
            };
            if (b.Corte == null)
                return reporte;

            // Foto actual de cada asignación.
            var instante = b.InstanteFoto(b.Corte.Value);
            var ventanas = await ProcesoActualizacionService.VentanasAsync(_db, b.Mapeo.LegadoPorEquipoSap, b.Reglas, instante.Date, b.Corte.Value);
            var semanales = (await _db.CalcDisponibilidadSemanal.AsNoTracking().Where(s => s.ReporteId == reporteId).ToListAsync())
                .ToDictionary(s => (s.AsignacionId, s.NoSemana));

            // Un grupo junta equipos de distinta naturaleza que juntos generan su disponibilidad. Una
            // fila que no es grupo y cuyos equipos están todos dentro de un grupo del mismo reporte se
            // muestra, pero "Todas" no la cuenta: ya la cuenta el grupo. La posición no importa
            // (respuesta del 02/10/2026: debajo de un grupo puede haber otros equipos).
            var grupoDe = ReglasPortal.MiembrosDeGrupos(b.Config.Asignaciones);
            var miembros = grupoDe.Keys.ToHashSet();

            var actuales = new List<(AsignacionDto Asig, int Equipos, int Disponibles)>();
            foreach (var a in b.Config.Asignaciones)
            {
                var equipos = a.Equipos.Where(b.Mapeo.CuentaParaDisponibilidad).ToList();
                var foto = CalculadoraDisponibilidad.FotosDelDia(equipos, a.CantRequerida, new[] { instante.Hour }, ventanas, instante.Date).Single();
                if (!miembros.Contains(a.AsignacionId))
                    actuales.Add((a, equipos.Count, foto.EqDisponible));
                reporte.Filas.Add(new FilaDisponibilidadDto
                {
                    AsignacionId = a.AsignacionId,
                    Asignacion = a.Nombre,
                    Negrita = a.Negrita,
                    Grupo = a.Grupo,
                    EsMiembro = miembros.Contains(a.AsignacionId),
                    EnGrupo = grupoDe.GetValueOrDefault(a.AsignacionId),
                    CantEquipo = equipos.Count,
                    EqRequerido = a.CantRequerida,
                    EqDisponible = foto.EqDisponible,
                    DispRealPct = foto.DispPct,
                    PotencialPct = foto.PotencialPct,
                    DispCompPct = a.DispCompPct,
                    Semaforo = b.Color(foto.DispPct),
                    Semanas = reporte.Semanas.Select(s => semanales.TryGetValue((a.AsignacionId, s.NoSemana), out var c)
                        ? new CeldaSemanaDto
                        {
                            NoSemana = s.NoSemana,
                            DispRealPct = c.DispRealPct,
                            DispRealMinPct = c.DispRealMinPct,
                            PotencialPct = c.PotencialPct,
                            DiasCalculados = c.DiasCalculados,
                            Semaforo = b.Color(c.DispRealPct)
                        }
                        : new CeldaSemanaDto { NoSemana = s.NoSemana }).ToList()
                });
            }

            if (actuales.Count > 0)
            {
                var totalPct = CalculadoraDisponibilidad.PorcentajeTotal(actuales.Select(x => ((decimal)x.Disponibles, x.Asig.CantRequerida)));
                reporte.Todas = new FilaDisponibilidadDto
                {
                    Asignacion = "Todas (Promedio)",
                    Negrita = true,
                    CantEquipo = actuales.Sum(x => x.Equipos),
                    EqRequerido = actuales.Sum(x => x.Asig.CantRequerida),
                    EqDisponible = actuales.Sum(x => x.Disponibles),
                    DispRealPct = totalPct,
                    PotencialPct = CalculadoraDisponibilidad.PorcentajeSinTope(actuales.Sum(x => x.Disponibles), actuales.Sum(x => x.Asig.CantRequerida)),
                    Semaforo = b.Color(totalPct),
                    Semanas = reporte.Semanas.Select(s =>
                    {
                        var celdas = b.Config.Asignaciones
                            .Where(a => !miembros.Contains(a.AsignacionId))
                            .Select(a => semanales.TryGetValue((a.AsignacionId, s.NoSemana), out var c) ? c : null)
                            .OfType<Core.Entities.Calculo.CalcDisponibilidadSemanal>()
                            .ToList();
                        if (celdas.Count == 0)
                            return new CeldaSemanaDto { NoSemana = s.NoSemana };
                        var pct = CalculadoraDisponibilidad.PorcentajeTotal(celdas.Select(c => (c.EqDisponibleProm, c.EqRequerido)));
                        return new CeldaSemanaDto
                        {
                            NoSemana = s.NoSemana,
                            DispRealPct = pct,
                            PotencialPct = CalculadoraDisponibilidad.PorcentajeSinTope(celdas.Sum(c => c.EqDisponibleProm), celdas.Sum(c => c.EqRequerido)),
                            DiasCalculados = celdas.Max(c => c.DiasCalculados),
                            Semaforo = b.Color(pct)
                        };
                    }).ToList()
                };
            }

            return reporte;
        }

        public async Task<DetalleSemanaDto> GetDetalleSemanaAsync(int reporteId, int asignacionId, int noSemana)
        {
            var b = await CargarBaseAsync(reporteId);
            var asignacion = b.Asignacion(asignacionId);
            var semana = b.Config.Semanas.FirstOrDefault(s => s.NoSemana == noSemana)
                         ?? throw new NotFoundException($"La semana {noSemana} no existe en el reporte {reporteId}.");

            var dias = await _db.CalcDisponibilidadDiaria.AsNoTracking()
                .Where(d => d.ReporteId == reporteId && d.AsignacionId == asignacionId && d.Fecha >= semana.FechaInicio && d.Fecha <= semana.FechaFin)
                .OrderBy(d => d.Fecha)
                .ToListAsync();
            var resumen = (await _db.CalcDisponibilidadSemanal.AsNoTracking()
                    .Where(s => s.ReporteId == reporteId && s.AsignacionId == asignacionId && s.NoSemana == noSemana)
                    .ToListAsync())
                .FirstOrDefault();

            var referencia = b.Referencia(Ahora);
            var ventanas = await ProcesoActualizacionService.VentanasAsync(_db, b.Mapeo.LegadoPorEquipoSap, b.Reglas, semana.FechaInicio, referencia);
            var finSemana = semana.FechaFin.AddDays(1);
            var delaSemana = asignacion.Equipos
                .SelectMany(e => ventanas[e])
                .Where(v => v.Desde < finSemana && v.Hasta > semana.FechaInicio)
                .ToList();

            return new DetalleSemanaDto
            {
                ReporteId = reporteId,
                AsignacionId = asignacionId,
                Asignacion = asignacion.Nombre,
                NoSemana = noSemana,
                FechaInicio = semana.FechaInicio,
                FechaFin = semana.FechaFin,
                UmbralRojo = b.Config.UmbralRojo,
                UmbralNaranja = b.Config.UmbralNaranja,
                Dias = dias.Select(d => new DiaDetalleDto
                {
                    Fecha = d.Fecha,
                    DispRealPct = d.DispRealPct,
                    DispCompPct = d.DispCompPct,
                    HrsDia = d.HrsDia,
                    HrsOtPlanificada = d.HrsOtPlanificada,
                    HrsOtPreventivo = d.HrsOtPreventivo,
                    HrsOtCorrectiva = d.HrsOtCorrectiva,
                    HrsRealEfec = d.HrsRealEfec,
                    HrsDiaDispComp = d.HrsDiaDispComp,
                    Semaforo = b.Color(d.DispRealPct)
                }).ToList(),
                Resumen = resumen == null ? null : new DiaDetalleDto
                {
                    Dias = resumen.DiasCalculados,
                    DispRealPct = resumen.DispRealPct,
                    DispCompPct = resumen.DispCompPct,
                    HrsDia = resumen.HrsDia,
                    HrsOtPlanificada = resumen.HrsOtPlanificada,
                    HrsOtPreventivo = resumen.HrsOtPreventivo,
                    HrsOtCorrectiva = resumen.HrsOtCorrectiva,
                    HrsRealEfec = resumen.HrsRealEfec,
                    HrsDiaDispComp = resumen.HrsDiaDispComp,
                    Semaforo = b.Color(resumen.DispRealPct)
                },
                Ordenes = await DetallarAsync(delaSemana, b, referencia)
            };
        }

        public async Task<List<HoraDisponibilidadDto>> GetHorasAsync(int reporteId, int asignacionId, DateTime fecha)
        {
            var b = await CargarBaseAsync(reporteId);
            var asignacion = b.Asignacion(asignacionId);
            var referencia = b.Referencia(Ahora);
            var dia = fecha.Date;
            if (dia > referencia.Date)
                return new List<HoraDisponibilidadDto>();

            var horas = b.Config.HorasActivas.Where(h => dia < referencia.Date || h <= referencia.Hour).ToList();
            var ventanas = await ProcesoActualizacionService.VentanasAsync(_db, b.Mapeo.LegadoPorEquipoSap, b.Reglas, dia, referencia);
            var equipos = asignacion.Equipos.Where(b.Mapeo.CuentaParaDisponibilidad).ToList();

            return CalculadoraDisponibilidad.FotosDelDia(equipos, asignacion.CantRequerida, horas, ventanas, dia)
                .OrderByDescending(f => f.Hora)
                .Select(f => new HoraDisponibilidadDto
                {
                    Hora = f.Hora,
                    CantEquipo = f.CantEquipo,
                    EqRequerido = f.EqRequerido,
                    EqDisponible = f.EqDisponible,
                    DispPct = f.DispPct,
                    PotencialPct = f.PotencialPct
                })
                .ToList();
        }

        public async Task<EquiposAsignacionDto> GetEquiposAsync(int reporteId, int asignacionId)
        {
            var b = await CargarBaseAsync(reporteId);
            var asignacion = b.Asignacion(asignacionId);
            var referencia = b.Referencia(Ahora);
            var instante = b.Corte == null ? referencia : b.InstanteFoto(b.Corte.Value);
            var ventanas = await ProcesoActualizacionService.VentanasAsync(_db, b.Mapeo.LegadoPorEquipoSap, b.Reglas, instante.Date, referencia);

            var activas = asignacion.Equipos
                .SelectMany(e => ventanas[e])
                .Where(v => v.Desde <= instante && instante < v.Hasta)
                .ToList();
            var detalle = (await DetallarAsync(activas, b, referencia)).ToLookup(o => o.Ficha, StringComparer.OrdinalIgnoreCase);

            var equipos = asignacion.Equipos.Select(legado =>
            {
                b.Mapeo.EquiposOracle.TryGetValue(legado, out var oracle);
                var enSap = b.Mapeo.EquipoSapPorLegado.TryGetValue(legado, out var sapId);
                var ordenes = detalle[legado].ToList();
                return new EquipoDetalleDto
                {
                    Ficha = legado,
                    EquipoCodigo = oracle?.EquipoCodigo,
                    Descripcion = oracle?.Descripcion ?? legado,
                    EquipoSapId = sapId,
                    Estado = !b.Mapeo.CuentaParaDisponibilidad(legado) ? "INACTIVO"
                        : ordenes.Count > 0 ? "FUERA_SERVICIO"
                        : !enSap ? "SIN_SAP"
                        : "DISPONIBLE",
                    Ordenes = ordenes
                };
            })
            .OrderBy(e => e.Estado switch { "FUERA_SERVICIO" => 0, "SIN_SAP" => 1, "DISPONIBLE" => 2, _ => 3 })
            .ThenBy(e => e.Ficha)
            .ToList();

            return new EquiposAsignacionDto
            {
                Asignacion = asignacion.Nombre,
                Fecha = instante,
                CantRequerida = asignacion.CantRequerida,
                Equipos = equipos
            };
        }

        /// <summary>Completa las ventanas con los datos de la orden y del aviso para mostrarlas.</summary>
        private async Task<List<OrdenDetalleDto>> DetallarAsync(List<VentanaFueraServicio> ventanas, Base b, DateTime referencia)
        {
            var ids = ventanas.Select(v => v.OrdenSapId).Distinct().ToList();
            var ordenes = new Dictionary<string, (string Clase, string Estado, string? Desc, string? AvisoId)>();
            foreach (var lote in ids.Chunk(TamanoLote))
            {
                foreach (var o in await _db.StgSapOrdenes.AsNoTracking().Where(o => lote.Contains(o.OrdenSapId))
                             .Select(o => new { o.OrdenSapId, o.ClaseOrden, o.EstadoOrden, o.Descripcion, o.AvisoSapId }).ToListAsync())
                    ordenes[o.OrdenSapId] = (o.ClaseOrden, o.EstadoOrden, o.Descripcion, o.AvisoSapId);
            }

            var avisoIds = ordenes.Values.Select(o => o.AvisoId).OfType<string>().Distinct().ToList();
            var avisos = new Dictionary<string, string?>();
            foreach (var lote in avisoIds.Chunk(TamanoLote))
            {
                foreach (var a in await _db.StgSapAvisos.AsNoTracking().Where(a => lote.Contains(a.AvisoSapId))
                             .Select(a => new { a.AvisoSapId, a.Texto }).ToListAsync())
                    avisos[a.AvisoSapId] = a.Texto;
            }

            return ventanas
                .OrderBy(v => v.Desde)
                .Select(v =>
                {
                    var o = ordenes.GetValueOrDefault(v.OrdenSapId);
                    var avisoTexto = o.AvisoId != null ? avisos.GetValueOrDefault(o.AvisoId) : null;
                    return new OrdenDetalleDto
                    {
                        OrdenSapId = v.OrdenSapId,
                        ClaseOrden = o.Clase ?? "",
                        Tipo = v.Tipo,
                        Ficha = v.CodigoLegado,
                        EquipoDescripcion = b.Mapeo.EquiposOracle.TryGetValue(v.CodigoLegado, out var e) ? e.Descripcion : null,
                        Desde = v.Desde,
                        Hasta = v.Hasta >= referencia ? null : v.Hasta,
                        EstadoActual = o.Estado ?? "",
                        Observacion = avisoTexto != null ? $"{o.Desc} (aviso {o.AvisoId}: {avisoTexto})" : o.Desc
                    };
                })
                .ToList();
        }
    }
}
