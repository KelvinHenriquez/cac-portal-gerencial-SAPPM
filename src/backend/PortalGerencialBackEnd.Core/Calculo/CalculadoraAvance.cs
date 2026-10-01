using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Entities.Calculo;

namespace PortalGerencialBackEnd.Core.Calculo
{
    /// <summary>Orden del programa de reparación de una familia (clase de avance, p. ej. ZC03).</summary>
    public record OrdenAvance(DateTime? FechaFinReal, bool EstaCerrada, decimal HhPlan, decimal HhReal);

    public enum FormulaAvanceReal
    {
        /// <summary>Horas planificadas de las órdenes cerradas / horas planificadas de todas.</summary>
        HhOrdenesCerradas,

        /// <summary>Órdenes cerradas / total de órdenes.</summary>
        OrdenesCerradas
    }

    /// <summary>
    /// Avance semanal de reparación por familia (sección 6.2). Programado = curva de Oracle
    /// acumulada semana a semana. Real = fórmula provisional configurable (FormulaAvanceReal),
    /// evaluada al cierre de cada semana — o a hoy, en la semana en curso y las futuras.
    /// </summary>
    public static class CalculadoraAvance
    {
        public static List<CalcAvanceSemanal> Calcular(
            string anioPrograma,
            string familiaCodigo,
            IEnumerable<CurvaSemanaDto> curvaFamilia,
            IReadOnlyCollection<OrdenAvance> ordenes,
            FormulaAvanceReal formula,
            DateTime hoy)
        {
            var hhPlanTotal = ordenes.Sum(o => o.HhPlan);
            var hhRealTotal = ordenes.Sum(o => o.HhReal);

            var resultado = new List<CalcAvanceSemanal>();
            var progAcumulado = 0m;

            foreach (var semana in curvaFamilia.OrderBy(c => c.NoSemana))
            {
                progAcumulado += semana.ActividadPct;
                var prog = Math.Min(100m, Math.Round(progAcumulado, 2));

                var corte = semana.FechaFinSemana.Date < hoy.Date ? semana.FechaFinSemana.Date : hoy.Date;
                var cerradas = ordenes
                    .Where(o => o.EstaCerrada && o.FechaFinReal is { } fin && fin.Date <= corte)
                    .ToList();

                var real = CalcularReal(formula, ordenes, cerradas, hhPlanTotal);

                resultado.Add(new CalcAvanceSemanal
                {
                    AnioPrograma = anioPrograma,
                    FamiliaCodigo = familiaCodigo,
                    Semana = semana.NoSemana,
                    FechaInicioSemana = semana.FechaInicioSemana.Date,
                    FechaFinSemana = semana.FechaFinSemana.Date,
                    ProgPct = prog,
                    RealPct = real,
                    PendPct = prog - real,
                    TotalOrdenes = ordenes.Count,
                    OrdenesCerradas = cerradas.Count,
                    HhPlan = hhPlanTotal,
                    HhReal = hhRealTotal
                });
            }

            return resultado;
        }

        private static decimal CalcularReal(
            FormulaAvanceReal formula,
            IReadOnlyCollection<OrdenAvance> todas,
            IReadOnlyCollection<OrdenAvance> cerradas,
            decimal hhPlanTotal)
        {
            if (todas.Count == 0)
                return 0m;

            // Sin horas planificadas cargadas en SAP no hay con qué ponderar: se cae a contar órdenes.
            if (formula == FormulaAvanceReal.HhOrdenesCerradas && hhPlanTotal > 0)
                return Math.Min(100m, Math.Round(100m * cerradas.Sum(o => o.HhPlan) / hhPlanTotal, 2));

            return Math.Min(100m, Math.Round(100m * cerradas.Count / todas.Count, 2));
        }
    }
}
