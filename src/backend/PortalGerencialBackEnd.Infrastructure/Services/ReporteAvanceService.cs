using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Core.Dtos.Avance;
using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Entities.Calculo;
using PortalGerencialBackEnd.Core.Entities.Sap;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    /// <summary>
    /// Avance de Órdenes de Trabajo, como el reporte de Oracle. La tabla sale de los resultados ya
    /// calculados (% de cada semana); los acumulados y las columnas a la fecha se arman aquí. Los
    /// drill-down leen el staging con las mismas reglas que el job.
    /// </summary>
    public class ReporteAvanceService : IReporteAvanceService
    {
        public const string Ordenes = "ORDENES";
        public const string Actividades = "ACTIVIDADES";

        private readonly PortalGerencialDbContext _db;
        private readonly IStructureSource _estructura;
        private readonly ISistemaService _sistema;
        private readonly TimeProvider _time;

        public ReporteAvanceService(PortalGerencialDbContext db, IStructureSource estructura, ISistemaService sistema, TimeProvider time)
        {
            _db = db;
            _estructura = estructura;
            _sistema = sistema;
            _time = time;
        }

        private async Task<ConfigAvanceDto> ConfigAsync(int reporteId) =>
            await _estructura.GetConfigAvanceAsync(reporteId)
            ?? throw new NotFoundException($"No existe el reporte de avance {reporteId}.");

        private async Task<DateTime?> CorteAsync() => (await _sistema.GetEstadoActualizacionAsync()).UltimaActualizacion;

        public async Task<ReporteAvanceDto> GetReporteAsync(int reporteId, string medida, string refPlan)
        {
            medida = string.Equals(medida, Actividades, StringComparison.OrdinalIgnoreCase) ? Actividades : Ordenes;
            refPlan = (refPlan ?? "").ToUpperInvariant() is "PROGRAMADO" or "NO_PROGRAMADO" ? refPlan!.ToUpperInvariant() : "EJECUTADO";
            if (medida == Ordenes) refPlan = "EJECUTADO";

            var config = await ConfigAsync(reporteId);
            var corte = await CorteAsync();
            var hoy = (corte ?? _time.GetLocalNow().DateTime).Date;
            var parametros = await ParametrosPortal.CargarAsync(_db);

            var filas = (await _db.CalcAvanceSemanal.AsNoTracking().Where(a => a.ReporteId == reporteId).ToListAsync())
                .ToLookup(a => a.FamiliaCodigo);

            // Columnas: las semanas de todas las familias, por número de semana del calendario.
            var semanas = config.Familias.SelectMany(f => f.Semanas)
                .GroupBy(s => s.NoSemana)
                .Select(g => g.First())
                .OrderBy(s => s.NoSemana)
                .Select(s => new SemanaAvanceDto
                {
                    NoSemana = s.NoSemana,
                    FechaInicio = s.FechaInicio,
                    FechaFin = s.FechaFin,
                    Momento = s.FechaFin < hoy ? "PASADA" : s.FechaInicio <= hoy ? "ACTUAL" : "FUTURA"
                })
                .ToList();

            decimal Real(CalcAvanceSemanal a) => medida == Ordenes ? a.RealOrdenesPct : refPlan switch
            {
                "PROGRAMADO" => a.RealActProgPct,
                "NO_PROGRAMADO" => a.RealActNoProgPct,
                _ => a.RealActPct
            };

            var reporte = new ReporteAvanceDto
            {
                ReporteId = config.ReporteId,
                Nombre = config.Nombre,
                PeriodoId = config.PeriodoId,
                PeriodoDescripcion = config.PeriodoDescripcion,
                FechaInicio = config.FechaInicio,
                FechaFin = config.FechaFin,
                FechaCorte = corte,
                Medida = medida,
                RefPlan = refPlan,
                BasePlan = ReglasPortal.BasePlan(parametros) == BasePlan.Dias ? "DIAS" : "ACTIVIDADES",
                Semanas = semanas
            };

            foreach (var familia in config.Familias)
            {
                var calc = filas[familia.FamiliaCodigo].OrderBy(a => a.NoSemana).ToList();
                var porSemana = calc.ToDictionary(a => a.NoSemana);
                var columnas = CalculadoraAvance.ColumnasALaFecha(
                    calc.Select(a => (a.FechaInicio, a.FechaFin, a.ProgPct, a.FechaInicio <= hoy ? Real(a) : 0m)).ToList(), hoy);
                var transcurridas = calc.Where(a => a.FechaInicio <= hoy).ToList();

                decimal progAcum = 0, realAcum = 0;
                var celdas = new List<CeldaAvanceDto>();
                foreach (var s in semanas)
                {
                    if (!porSemana.TryGetValue(s.NoSemana, out var a))
                    {
                        celdas.Add(new CeldaAvanceDto { NoSemana = s.NoSemana });
                        continue;
                    }
                    progAcum += a.ProgPct;
                    var futura = s.Momento == "FUTURA";
                    if (!futura) realAcum += Real(a);
                    celdas.Add(new CeldaAvanceDto
                    {
                        NoSemana = s.NoSemana,
                        EnPlan = true,
                        ProgPct = a.ProgPct,
                        RealPct = futura ? null : Real(a),
                        ProgAcumPct = Math.Round(progAcum, 2),
                        RealAcumPct = futura ? null : Math.Round(realAcum, 2)
                    });
                }

                reporte.Familias.Add(new FilaAvanceDto
                {
                    FamiliaCodigo = familia.FamiliaCodigo,
                    FamiliaNombre = familia.FamiliaNombre,
                    CantEquipo = familia.CantEquipo,
                    FechaEstInicio = familia.FechaEstInicio,
                    FechaEstFin = familia.FechaEstFin,
                    TProg = Math.Round(columnas.TProg, 2),
                    ProgCierre = Math.Round(columnas.ProgCierre, 2),
                    TotalEjec = Math.Round(columnas.TotalEjec, 2),
                    ProgAct = Math.Round(columnas.ProgAct, 2),
                    PendAnt = Math.Round(columnas.PendAnt, 2),
                    TPendR = Math.Round(columnas.TPendR, 2),
                    OrdenesTotal = calc.FirstOrDefault()?.OrdenesTotal ?? 0,
                    OrdenesCerradas = transcurridas.Sum(a => a.OrdenesCerradas),
                    ActTotal = calc.FirstOrDefault()?.ActTotal ?? 0,
                    ActTerminadas = transcurridas.Sum(a => a.ActTerminadas),
                    Semanas = celdas
                });
            }

            return reporte;
        }

        /// <summary>Órdenes del programa de una familia, con sus actividades, y el mapeo de equipos.</summary>
        private async Task<(List<StgSapOrden> Ordenes, MapeoEquipos Mapeo, ReglasVentana Reglas, FamiliaAvanceDto Familia)>
            ProgramaAsync(ConfigAvanceDto config, string familiaCodigo)
        {
            var familia = config.Familias.FirstOrDefault(f => f.FamiliaCodigo == familiaCodigo)
                          ?? throw new NotFoundException($"La familia '{familiaCodigo}' no está en el reporte {config.ReporteId}.");
            var parametros = await ParametrosPortal.CargarAsync(_db);
            var reglas = ReglasPortal.Ventana(parametros);
            var clases = ReglasPortal.ClasesAvance(parametros).ToList();
            var mapeo = await MapeoEquipos.CargarAsync(_db, _estructura);

            var equiposPrograma = ReglasPortal.EquiposDelPrograma(familia, mapeo);
            var equiposSap = equiposPrograma
                .Where(mapeo.EquipoSapPorClave.ContainsKey)
                .Select(legado => mapeo.EquipoSapPorClave[legado])
                .ToList();

            var ordenes = new List<StgSapOrden>();
            foreach (var lote in equiposSap.Chunk(900))
            {
                ordenes.AddRange(await _db.StgSapOrdenes.AsNoTracking()
                    .Where(o => o.EquipoSapId != null && lote.Contains(o.EquipoSapId) && clases.Contains(o.ClaseOrden))
                    .Include(o => o.Operaciones)
                    .ToListAsync());
            }

            ReglasPortal.AplicarHojaRuta(ordenes, ReglasPortal.UsuariosHojaRuta(parametros));
            return (ordenes.Where(o => ReglasPortal.EsDelPrograma(o, mapeo, config, equiposPrograma, reglas)).ToList(), mapeo, reglas, familia);
        }

        public async Task<DetalleSemanaAvanceDto> GetDetalleSemanaAsync(int reporteId, string familiaCodigo, int noSemana)
        {
            var config = await ConfigAsync(reporteId);
            var (ordenes, mapeo, reglas, familia) = await ProgramaAsync(config, familiaCodigo);
            var calendario = ReglasPortal.PlanEnCalendario(config, familia);
            var plan = calendario.FirstOrDefault(s => s.NoSemana == noSemana)
                       ?? throw new NotFoundException($"La semana {noSemana} no está en el calendario de {config.Nombre}.");
            var calc = (await _db.CalcAvanceSemanal.AsNoTracking()
                    .Where(a => a.ReporteId == reporteId && a.FamiliaCodigo == familiaCodigo && a.NoSemana == noSemana)
                    .ToListAsync())
                .FirstOrDefault();

            // Misma regla del cálculo: lo terminado antes del programa cuenta en la primera semana y
            // lo terminado después, en la última.
            var primera = calendario.Min(s => s.NoSemana) == noSemana;
            var ultima = calendario.Max(s => s.NoSemana) == noSemana;
            bool EnSemana(DateTime? f) => f is { } d
                && (d.Date >= plan.FechaInicio || primera)
                && (d.Date <= plan.FechaFin || ultima);

            string Ficha(StgSapOrden o) => o.EquipoSapId != null && mapeo.ClavePorEquipoSap.TryGetValue(o.EquipoSapId, out var l) ? l : "";

            var actividades = ordenes
                .SelectMany(o => o.Operaciones.Select(op => (Orden: o, Op: op, Terminada: ReglasPortal.Terminada(op))))
                .Where(x => EnSemana(x.Terminada))
                .Select(x => new ActividadDto
                {
                    OrdenSapId = x.Orden.OrdenSapId,
                    Operacion = x.Op.Operacion,
                    Ficha = Ficha(x.Orden),
                    Texto = x.Op.Texto,
                    Sistema = x.Op.Sistema,
                    Subsistema = x.Op.Subsistema,
                    Componente = x.Op.Componente,
                    Programada = x.Op.Programada,
                    FechaTerminada = x.Terminada,
                    Dias = Math.Round(ReglasPortal.DiasActividad(x.Op), 2)
                })
                .OrderBy(a => a.FechaTerminada).ThenBy(a => a.OrdenSapId).ThenBy(a => a.Operacion)
                .ToList();

            var cerradas = ordenes
                .Select(o => (Orden: o, Cierre: ReglasPortal.OrdenTerminada(o, reglas)))
                .Where(x => EnSemana(x.Cierre))
                .Select(x => new OrdenCerradaDto { OrdenSapId = x.Orden.OrdenSapId, Ficha = Ficha(x.Orden), Descripcion = x.Orden.Descripcion, FechaCierre = x.Cierre!.Value })
                .OrderBy(o => o.FechaCierre)
                .ToList();

            return new DetalleSemanaAvanceDto
            {
                FamiliaCodigo = familiaCodigo,
                FamiliaNombre = familia.FamiliaNombre,
                NoSemana = noSemana,
                FechaInicio = plan.FechaInicio,
                FechaFin = plan.FechaFin,
                Programado = calc?.ProgPct ?? 0,
                RealEjProg = calc?.RealActProgPct ?? 0,
                RealEjNoProg = calc?.RealActNoProgPct ?? 0,
                RealEjecutado = calc?.RealActPct ?? 0,
                RealOrdenes = calc?.RealOrdenesPct ?? 0,
                Dias = Enumerable.Range(0, (plan.FechaFin - plan.FechaInicio).Days + 1)
                    .Select(i => plan.FechaInicio.AddDays(i))
                    .Select(d => new DiaAvanceDto
                    {
                        Fecha = d,
                        ActTerminadas = actividades.Count(a => a.FechaTerminada!.Value.Date == d),
                        OrdenesCerradas = cerradas.Count(o => o.FechaCierre.Date == d)
                    }).ToList(),
                Actividades = actividades,
                OrdenesCerradas = cerradas
            };
        }

        public async Task<AnalisisAvanceDto> GetAnalisisAsync(int reporteId, string familiaCodigo)
        {
            var config = await ConfigAsync(reporteId);
            var (ordenes, mapeo, _, familia) = await ProgramaAsync(config, familiaCodigo);

            static T Fila<T>(T fila, IReadOnlyCollection<StgSapOperacion> ops) where T : AnalisisFilaDto
            {
                var terminadas = ops.Where(o => ReglasPortal.Terminada(o) != null).ToList();
                fila.ActTotal = ops.Count;
                fila.ActTerminadas = terminadas.Count;
                fila.DiasActProg = Math.Round(ops.Sum(ReglasPortal.DiasActividad), 2);
                fila.DiasActTerm = Math.Round(terminadas.Sum(ReglasPortal.DiasActividad), 2);
                fila.AvancePct = fila.DiasActProg == 0 ? 0 : Math.Round(100 * fila.DiasActTerm / fila.DiasActProg, 0);
                return fila;
            }

            var todas = ordenes.SelectMany(o => o.Operaciones).ToList();
            return new AnalisisAvanceDto
            {
                FamiliaCodigo = familiaCodigo,
                FamiliaNombre = familia.FamiliaNombre,
                Total = Fila(new AnalisisFilaDto { Nombre = "Todos" }, todas),
                Equipos = ordenes
                    .Select(o =>
                    {
                        var ficha = o.EquipoSapId != null && mapeo.ClavePorEquipoSap.TryGetValue(o.EquipoSapId, out var l) ? l : "";
                        var desc = mapeo.EquiposOracle.TryGetValue(ficha, out var e) ? e.Descripcion : ficha;
                        return Fila(new AnalisisEquipoDto
                        {
                            Nombre = desc,
                            Ficha = ficha,
                            OrdenSapId = o.OrdenSapId,
                            Estado = o.EstadoOrden,
                            FechaRealInicio = o.FechaInicioReal,
                            FechaRealFin = o.FechaCierre ?? o.FechaFinReal
                        }, o.Operaciones.ToList());
                    })
                    .OrderBy(e => e.Ficha)
                    .ToList(),
                Sistemas = todas.GroupBy(o => o.Sistema ?? "(sin sistema)")
                    .Select(g => Fila(new AnalisisFilaDto { Nombre = g.Key }, g.ToList()))
                    .OrderBy(s => s.Nombre)
                    .ToList()
            };
        }
    }
}
