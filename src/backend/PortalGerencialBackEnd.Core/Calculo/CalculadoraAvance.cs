using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Entities.Calculo;

namespace PortalGerencialBackEnd.Core.Calculo
{
    /// <summary>Orden del programa de una familia; FechaCierre solo si está cerrada.</summary>
    public record OrdenAvance(string OrdenSapId, DateTime? FechaCierre);

    /// <summary>
    /// Actividad (operación) de una orden del programa. Terminada cuando tiene horas reales y
    /// cuenta en la semana en que se confirmaron; sus días salen de sus fechas plan.
    /// </summary>
    public record ActividadAvance(decimal Dias, DateTime? FechaTerminada, bool Programada);

    public enum BasePlan { Actividades, Dias }

    /// <summary>Columnas "a la fecha" del reporte de Oracle (verificadas con sus capturas).</summary>
    public record ColumnasAvance(decimal TProg, decimal ProgCierre, decimal TotalEjec, decimal ProgAct, decimal PendAnt, decimal TPendR);

    /// <summary>
    /// Avance semanal por familia, con el % de cada semana (no acumulado) como las columnas del
    /// reporte de Oracle. Programado = plan de Oracle; real por órdenes cerradas y por actividades.
    /// </summary>
    public static class CalculadoraAvance
    {
        public static List<CalcAvanceSemanal> Calcular(
            int reporteId,
            string familiaCodigo,
            IReadOnlyList<SemanaPlanDto> plan,
            BasePlan basePlan,
            IReadOnlyCollection<OrdenAvance> ordenes,
            IReadOnlyCollection<ActividadAvance> actividades)
        {
            var semanas = plan.OrderBy(s => s.NoSemana).ToList();
            if (semanas.Count == 0)
                return new List<CalcAvanceSemanal>();

            var totalPlan = semanas.Sum(s => basePlan == BasePlan.Dias ? s.Dias : s.Actividad);
            var diasTotal = actividades.Sum(a => a.Dias);

            // Semana a la que cae una fecha: lo terminado antes del programa cuenta en la primera
            // semana y lo terminado después, en la última (sigue siendo avance del programa).
            int Indice(DateTime fecha)
            {
                var d = fecha.Date;
                if (d < semanas[0].FechaInicio) return 0;
                var i = semanas.FindIndex(s => s.FechaInicio <= d && d <= s.FechaFin);
                return i >= 0 ? i : semanas.Count - 1;
            }

            var cerradasPorSemana = ordenes.Where(o => o.FechaCierre != null).GroupBy(o => Indice(o.FechaCierre!.Value))
                .ToDictionary(g => g.Key, g => g.Count());
            var terminadasPorSemana = actividades.Where(a => a.FechaTerminada != null).GroupBy(a => Indice(a.FechaTerminada!.Value))
                .ToDictionary(g => g.Key, g => g.ToList());

            return semanas.Select((s, i) =>
            {
                var terminadas = terminadasPorSemana.GetValueOrDefault(i, new List<ActividadAvance>());
                var diasTerm = terminadas.Sum(a => a.Dias);
                var cerradas = cerradasPorSemana.GetValueOrDefault(i);
                return new CalcAvanceSemanal
                {
                    ReporteId = reporteId,
                    FamiliaCodigo = familiaCodigo,
                    NoSemana = s.NoSemana,
                    FechaInicio = s.FechaInicio,
                    FechaFin = s.FechaFin,
                    ProgPct = Pct(basePlan == BasePlan.Dias ? s.Dias : s.Actividad, totalPlan),
                    OrdenesTotal = ordenes.Count,
                    OrdenesCerradas = cerradas,
                    RealOrdenesPct = Pct(cerradas, ordenes.Count),
                    ActTotal = actividades.Count,
                    ActTerminadas = terminadas.Count,
                    DiasActTotal = Math.Round(diasTotal, 2),
                    DiasActTerminadas = Math.Round(diasTerm, 2),
                    RealActPct = Pct(diasTerm, diasTotal),
                    RealActProgPct = Pct(terminadas.Where(a => a.Programada).Sum(a => a.Dias), diasTotal),
                    RealActNoProgPct = Pct(terminadas.Where(a => !a.Programada).Sum(a => a.Dias), diasTotal)
                };
            }).ToList();
        }

        /// <summary>
        /// Columnas del reporte a la fecha <paramref name="hoy"/>:
        /// T. Prog. = total del plan; Prog. Cierre = programado hasta la semana anterior; Prog. Act.
        /// = programado de la semana actual; Total Ejec. = real hasta hoy; Pend. Ant. = máx(0, Prog.
        /// Cierre − real hasta la semana anterior); T. Pend. R. = máx(0, Pend. Ant. + Prog. Act. − real
        /// de la semana actual).
        /// </summary>
        public static ColumnasAvance ColumnasALaFecha(
            IReadOnlyCollection<(DateTime FechaInicio, DateTime FechaFin, decimal Prog, decimal Real)> semanas, DateTime hoy)
        {
            var d = hoy.Date;
            var anteriores = semanas.Where(s => s.FechaFin < d).ToList();
            var actual = semanas.FirstOrDefault(s => s.FechaInicio <= d && d <= s.FechaFin);
            var progCierre = anteriores.Sum(s => s.Prog);
            var realAnterior = anteriores.Sum(s => s.Real);
            var progAct = actual.Prog;
            var realAct = actual.Real;
            var pendAnt = Math.Max(0, progCierre - realAnterior);
            return new ColumnasAvance(
                TProg: semanas.Sum(s => s.Prog),
                ProgCierre: progCierre,
                TotalEjec: realAnterior + realAct,
                ProgAct: progAct,
                PendAnt: pendAnt,
                TPendR: Math.Max(0, pendAnt + progAct - realAct));
        }

        private static decimal Pct(decimal parte, decimal total) => total <= 0 ? 0m : Math.Round(100m * parte / total, 2);
    }
}
