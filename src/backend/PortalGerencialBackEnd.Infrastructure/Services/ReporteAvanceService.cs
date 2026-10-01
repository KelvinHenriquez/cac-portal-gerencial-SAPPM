using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Dtos.Avance;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    /// <summary>
    /// Pantalla Avance de Reparación (sección 8.3). Cada familia tiene su propia ventana dentro del
    /// programa, así que las columnas son semanas de calendario (lunes a domingo) del programa
    /// completo y cada familia llena las que le tocan.
    /// </summary>
    public class ReporteAvanceService : IReporteAvanceService
    {
        private readonly PortalGerencialDbContext _db;
        private readonly IStructureSource _estructura;
        private readonly TimeProvider _time;

        public ReporteAvanceService(PortalGerencialDbContext db, IStructureSource estructura, TimeProvider time)
        {
            _db = db;
            _estructura = estructura;
            _time = time;
        }

        public async Task<ReporteAvanceDto> GetAvanceAsync(string anioPrograma)
        {
            var programa = (await _estructura.GetProgramasAsync()).FirstOrDefault(p => p.AnioPrograma == anioPrograma)
                           ?? throw new NotFoundException($"No existe el programa de reparación '{anioPrograma}'.");
            var familias = await _estructura.GetFamiliasAsync(anioPrograma);

            var filas = await _db.CalcAvanceSemanal.AsNoTracking()
                .Where(a => a.AnioPrograma == anioPrograma)
                .ToListAsync();

            // El "ejecutado" es a la fecha del último cálculo, no a la de hoy: si el job lleva días
            // sin correr, el reporte no debe aparentar datos de hoy (el encabezado ya lo advierte).
            var fechaCorte = filas.Count == 0 ? (DateTime?)null : filas.Max(a => a.GeneradoEn).Date;
            var corte = fechaCorte ?? _time.GetLocalNow().Date;
            var parametros = await ParametrosPortal.CargarAsync(_db);

            var semanas = new List<SemanaAvanceDto>();
            for (var lunes = Lunes(programa.FechaInicio); lunes <= programa.FechaFin; lunes = lunes.AddDays(7))
            {
                semanas.Add(new SemanaAvanceDto
                {
                    Lunes = lunes,
                    Domingo = lunes.AddDays(6),
                    SemanaIso = ISOWeek.GetWeekOfYear(lunes),
                    Momento = lunes.AddDays(6) < corte ? "PASADA" : lunes <= corte ? "ACTUAL" : "FUTURA"
                });
            }

            var porFamilia = filas.ToLookup(a => a.FamiliaCodigo);
            var reporte = new ReporteAvanceDto
            {
                AnioPrograma = anioPrograma,
                FechaInicio = programa.FechaInicio,
                FechaFin = programa.FechaFin,
                FechaCorte = fechaCorte,
                FormulaReal = parametros.Texto(ClavesParametro.FormulaAvanceReal, "HH_ORDENES_CERRADAS"),
                Semanas = semanas
            };

            foreach (var familia in familias.OrderBy(f => f.FechaEstInicio).ThenBy(f => f.FamiliaNombre))
            {
                // Semana de la curva → lunes de calendario (la primera semana de una familia puede
                // empezar a mitad de semana, cuando su ventana no arranca en lunes).
                var semanasFamilia = porFamilia[familia.FamiliaCodigo]
                    .GroupBy(a => Lunes(a.FechaInicioSemana))
                    .ToDictionary(g => g.Key, g => g.OrderBy(a => a.Semana).Last());

                // Fila "a la fecha": la semana de la curva que contiene el corte; antes de que
                // empiece la familia, nada; después de que termine, la última.
                var aLaFecha = porFamilia[familia.FamiliaCodigo]
                    .Where(a => a.FechaInicioSemana <= corte)
                    .OrderBy(a => a.Semana)
                    .LastOrDefault();

                reporte.Familias.Add(new FilaAvanceDto
                {
                    FamiliaCodigo = familia.FamiliaCodigo,
                    FamiliaNombre = familia.FamiliaNombre,
                    CantEquipo = familia.CantEquipo,
                    FechaEstInicio = familia.FechaEstInicio,
                    FechaEstFin = familia.FechaEstFin,
                    TotalProgramadoPct = aLaFecha?.ProgPct ?? 0,
                    TotalEjecutadoPct = aLaFecha?.RealPct ?? 0,
                    DiferenciaPct = (aLaFecha?.RealPct ?? 0) - (aLaFecha?.ProgPct ?? 0),
                    TotalOrdenes = aLaFecha?.TotalOrdenes ?? porFamilia[familia.FamiliaCodigo].FirstOrDefault()?.TotalOrdenes ?? 0,
                    OrdenesCerradas = aLaFecha?.OrdenesCerradas ?? 0,
                    HhPlan = aLaFecha?.HhPlan ?? 0,
                    HhReal = aLaFecha?.HhReal ?? 0,
                    Semanas = semanas.Select(s => semanasFamilia.TryGetValue(s.Lunes, out var a)
                        ? new CeldaAvanceDto
                        {
                            EnVentana = true,
                            NoSemana = a.Semana,
                            ProgPct = a.ProgPct,
                            RealPct = s.Momento == "FUTURA" ? null : a.RealPct
                        }
                        : new CeldaAvanceDto { EnVentana = false }).ToList()
                });
            }

            return reporte;
        }

        private static DateTime Lunes(DateTime fecha) => fecha.Date.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));
    }
}
