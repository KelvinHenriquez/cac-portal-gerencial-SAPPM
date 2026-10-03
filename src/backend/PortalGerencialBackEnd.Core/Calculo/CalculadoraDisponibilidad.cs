using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Entities.Calculo;

namespace PortalGerencialBackEnd.Core.Calculo
{
    /// <summary>Reglas configurables (PARAMETRO_PGPM) para decidir cuándo una orden deja un equipo fuera.</summary>
    public record ReglasVentana(
        ISet<string> ClasesIncluidas,
        bool InicioEnCreacion,
        bool CorrectivasSegunAviso,
        ISet<string> EstadosCerrados,
        ISet<string> EstadosExcluidos,
        IReadOnlyDictionary<string, string> TipoPorClase,
        bool BaseEquipos = false)
    {
        public string Tipo(string claseOrden) =>
            TipoPorClase.TryGetValue(claseOrden, out var t) ? t : TiposOrden.Planificada;
    }

    public record AvisoDisponibilidad(bool Parada, DateTime? InicioAveria, DateTime? FinAveria);

    /// <summary>Una orden ya cruzada con Oracle (se conoce el código legado de su equipo).</summary>
    public record OrdenDisponibilidad(
        string OrdenSapId,
        string CodigoLegado,
        string ClaseOrden,
        string EstadoOrden,
        DateTime FechaCreacion,
        DateTime? FechaInicioReal,
        DateTime? FechaFinReal,
        DateTime? FechaCierre,
        AvisoDisponibilidad? Aviso);

    /// <summary>Tramo en que una orden deja a su equipo fuera de servicio.</summary>
    public record VentanaFueraServicio(string CodigoLegado, string OrdenSapId, string Tipo, DateTime Desde, DateTime Hasta);

    /// <summary>Foto de una hora (detalle "Disponibilidad" del reporte de Oracle).</summary>
    public record DisponibilidadHora(int Hora, int CantEquipo, int EqRequerido, int EqDisponible, decimal DispPct, decimal PotencialPct);

    /// <summary>
    /// Disponibilidad como la mide el reporte de Oracle: una foto por cada hora activa del reporte
    /// (equipos sin ninguna orden abierta en ese instante), el día como promedio de sus fotos y la
    /// semana como promedio de sus días. Lógica pura, sin base de datos.
    /// </summary>
    public static class CalculadoraDisponibilidad
    {
        /// <summary>
        /// Desde la creación de la orden (o el inicio real, según reglas) hasta su cierre técnico, o
        /// hasta ahora si sigue abierta. Una correctiva con aviso, si así lo dicen las reglas, solo
        /// cuenta si el aviso indica parada, y entonces desde el inicio hasta el fin de la avería.
        /// </summary>
        public static VentanaFueraServicio? Ventana(OrdenDisponibilidad o, ReglasVentana r, DateTime ahora)
        {
            if (r.EstadosExcluidos.Contains(o.EstadoOrden))
                return null;
            if (r.ClasesIncluidas.Count > 0 && !r.ClasesIncluidas.Contains(o.ClaseOrden))
                return null;

            var tipo = r.Tipo(o.ClaseOrden);
            DateTime? desde = r.InicioEnCreacion ? o.FechaCreacion : o.FechaInicioReal;
            if (desde == null)
                return null;

            var cerrada = r.EstadosCerrados.Contains(o.EstadoOrden);
            var hasta = o.FechaCierre ?? (cerrada ? o.FechaFinReal ?? desde.Value : ahora);

            if (tipo == TiposOrden.Correctiva && r.CorrectivasSegunAviso && o.Aviso != null)
            {
                if (!o.Aviso.Parada)
                    return null;
                desde = o.Aviso.InicioAveria ?? desde;
                hasta = o.Aviso.FinAveria ?? hasta;
            }

            if (hasta > ahora)
                hasta = ahora;
            return hasta > desde.Value ? new VentanaFueraServicio(o.CodigoLegado, o.OrdenSapId, tipo, desde.Value, hasta) : null;
        }

        /// <summary>Fotos de las horas activas de un día para una asignación.</summary>
        public static List<DisponibilidadHora> FotosDelDia(
            IReadOnlyCollection<string> equipos, int requeridos, IEnumerable<int> horasActivas,
            ILookup<string, VentanaFueraServicio> ventanas, DateTime dia, bool baseEquipos = false)
        {
            // Base del %: requeridos (decisión del 30/09/2026) o equipos de la asignación (como Oracle:
            // PORCIENTO_HORA = disponibles / equipos). El potencial siempre es sobre requeridos.
            return horasActivas.Select(h =>
            {
                var instante = dia.Date.AddHours(h);
                var fuera = equipos.Count(e => ventanas[e].Any(v => v.Desde <= instante && instante < v.Hasta));
                var disponibles = equipos.Count - fuera;
                return new DisponibilidadHora(h, equipos.Count, requeridos, disponibles,
                    Porcentaje(disponibles, baseEquipos ? equipos.Count : requeridos), PorcentajeSinTope(disponibles, requeridos));
            }).ToList();
        }

        /// <summary>Un día de una asignación: promedio de las fotos horarias + horas por tipo de orden.</summary>
        public static CalcDisponibilidadDiaria CalcularDia(
            int reporteId, AsignacionDto asignacion, IReadOnlyCollection<string> equipos, IReadOnlyCollection<int> horasActivas,
            ILookup<string, VentanaFueraServicio> ventanas, DateTime dia, bool baseEquipos = false)
        {
            var fotos = FotosDelDia(equipos, asignacion.CantRequerida, horasActivas, ventanas, dia, baseEquipos);
            var (planificada, preventivo, correctiva) = HorasPorTipo(equipos, ventanas, dia);
            var hrsDia = equipos.Count * 24m;

            return new CalcDisponibilidadDiaria
            {
                ReporteId = reporteId,
                AsignacionId = asignacion.AsignacionId,
                Fecha = dia.Date,
                CantEquipo = equipos.Count,
                EqRequerido = asignacion.CantRequerida,
                EqDisponibleProm = fotos.Count == 0 ? equipos.Count : Math.Round((decimal)fotos.Average(f => f.EqDisponible), 2),
                DispRealPct = fotos.Count == 0 ? Porcentaje(equipos.Count, baseEquipos ? equipos.Count : asignacion.CantRequerida) : Math.Round(fotos.Average(f => f.DispPct), 2),
                PotencialPct = fotos.Count == 0 ? PorcentajeSinTope(equipos.Count, asignacion.CantRequerida) : Math.Round(fotos.Average(f => f.PotencialPct), 2),
                DispCompPct = asignacion.DispCompPct,
                HorasMedidas = fotos.Count,
                HrsDia = hrsDia,
                HrsOtPlanificada = planificada,
                HrsOtPreventivo = preventivo,
                HrsOtCorrectiva = correctiva,
                HrsRealEfec = hrsDia - correctiva,
                HrsDiaDispComp = Math.Round(hrsDia * asignacion.DispCompPct / 100m, 2)
            };
        }

        /// <summary>
        /// Horas-equipo del día dentro de órdenes, por tipo. Si un equipo tiene órdenes de distinto
        /// tipo a la vez, la hora se cuenta una sola vez, con prioridad correctiva > preventivo >
        /// planificada.
        /// </summary>
        public static (decimal Planificada, decimal Preventivo, decimal Correctiva) HorasPorTipo(
            IEnumerable<string> equipos, ILookup<string, VentanaFueraServicio> ventanas, DateTime dia)
        {
            var inicio = dia.Date;
            var fin = inicio.AddDays(1);
            double plan = 0, prev = 0, corr = 0;

            foreach (var e in equipos)
            {
                var delDia = ventanas[e].Where(v => v.Desde < fin && v.Hasta > inicio)
                    .Select(v => (v.Tipo, Desde: v.Desde < inicio ? inicio : v.Desde, Hasta: v.Hasta > fin ? fin : v.Hasta))
                    .ToList();
                if (delDia.Count == 0)
                    continue;

                var c = Unir(delDia.Where(v => v.Tipo == TiposOrden.Correctiva).Select(v => (v.Desde, v.Hasta)));
                var p = Restar(Unir(delDia.Where(v => v.Tipo == TiposOrden.Preventivo).Select(v => (v.Desde, v.Hasta))), c);
                var l = Restar(Restar(Unir(delDia.Where(v => v.Tipo == TiposOrden.Planificada).Select(v => (v.Desde, v.Hasta))), c), p);
                corr += Horas(c);
                prev += Horas(p);
                plan += Horas(l);
            }

            return (Math.Round((decimal)plan, 2), Math.Round((decimal)prev, 2), Math.Round((decimal)corr, 2));
        }

        /// <summary>Semana del calendario del reporte: promedio de sus días, horas sumadas.</summary>
        public static CalcDisponibilidadSemanal AgregarSemana(int reporteId, int asignacionId, SemanaCalendarioDto semana,
            IReadOnlyCollection<CalcDisponibilidadDiaria> dias)
        {
            var ultimo = dias.OrderBy(d => d.Fecha).Last();
            return new CalcDisponibilidadSemanal
            {
                ReporteId = reporteId,
                AsignacionId = asignacionId,
                NoSemana = semana.NoSemana,
                FechaInicio = semana.FechaInicio,
                FechaFin = semana.FechaFin,
                DiasCalculados = dias.Count,
                CantEquipo = ultimo.CantEquipo,
                EqRequerido = ultimo.EqRequerido,
                EqDisponibleProm = Math.Round(dias.Average(d => d.EqDisponibleProm), 2),
                DispRealPct = Math.Round(dias.Average(d => d.DispRealPct), 2),
                DispRealMinPct = dias.Min(d => d.DispRealPct),
                PotencialPct = Math.Round(dias.Average(d => d.PotencialPct), 2),
                DispCompPct = ultimo.DispCompPct,
                HrsDia = dias.Sum(d => d.HrsDia),
                HrsOtPlanificada = dias.Sum(d => d.HrsOtPlanificada),
                HrsOtPreventivo = dias.Sum(d => d.HrsOtPreventivo),
                HrsOtCorrectiva = dias.Sum(d => d.HrsOtCorrectiva),
                HrsRealEfec = dias.Sum(d => d.HrsRealEfec),
                HrsDiaDispComp = dias.Sum(d => d.HrsDiaDispComp)
            };
        }

        /// <summary>Parte / total en %, topado en 100 (decisión del 30/09/2026). Sin requeridos: 100.</summary>
        public static decimal Porcentaje(decimal parte, int total) =>
            total <= 0 ? 100m : Math.Min(100m, Math.Round(100m * parte / total, 2));

        /// <summary>Potencial %: parte / total sin tope (el reporte de Oracle muestra 101 %).</summary>
        public static decimal PorcentajeSinTope(decimal parte, int total) =>
            total <= 0 ? 100m : Math.Round(100m * parte / total, 2);

        /// <summary>
        /// Disponibilidad de un grupo (fila "Todas"): total disponible / total requerido, contando de
        /// cada asignación como máximo sus requeridos: lo que sobra en una no tapa lo que falta en otra
        /// (decisión del 30/09/2026: "menos engañoso").
        /// </summary>
        public static decimal PorcentajeTotal(IEnumerable<(decimal Disponibles, int Requeridos)> grupos)
        {
            var lista = grupos.ToList();
            var requeridos = lista.Sum(f => f.Requeridos);
            if (requeridos <= 0)
                return 100m;
            var cubiertos = lista.Sum(f => Math.Min(f.Disponibles, f.Requeridos));
            return Math.Min(100m, Math.Round(100m * cubiertos / requeridos, 2));
        }

        /// <summary>
        /// Fila "Todas" con la base del porcentaje elegida: sobre requeridos (regla de arriba) o, como
        /// Oracle, total disponible / total de equipos.
        /// </summary>
        public static decimal PorcentajeTotal(IEnumerable<(decimal Disponibles, int Requeridos, int Equipos)> filas, bool baseEquipos)
        {
            var lista = filas.ToList();
            if (!baseEquipos)
                return PorcentajeTotal(lista.Select(f => (f.Disponibles, f.Requeridos)));
            var equipos = lista.Sum(f => f.Equipos);
            return equipos <= 0 ? 100m : Math.Min(100m, Math.Round(100m * lista.Sum(f => f.Disponibles) / equipos, 2));
        }

        // ------------------------------------------------------------ intervalos

        private static List<(DateTime Desde, DateTime Hasta)> Unir(IEnumerable<(DateTime Desde, DateTime Hasta)> tramos)
        {
            var resultado = new List<(DateTime Desde, DateTime Hasta)>();
            foreach (var t in tramos.OrderBy(t => t.Desde))
            {
                if (resultado.Count > 0 && t.Desde <= resultado[^1].Hasta)
                {
                    if (t.Hasta > resultado[^1].Hasta)
                        resultado[^1] = (resultado[^1].Desde, t.Hasta);
                }
                else
                {
                    resultado.Add(t);
                }
            }
            return resultado;
        }

        private static List<(DateTime Desde, DateTime Hasta)> Restar(
            List<(DateTime Desde, DateTime Hasta)> a, List<(DateTime Desde, DateTime Hasta)> b)
        {
            var resultado = new List<(DateTime Desde, DateTime Hasta)>();
            foreach (var t in a)
            {
                var piezas = new List<(DateTime Desde, DateTime Hasta)> { t };
                foreach (var q in b)
                {
                    piezas = piezas.SelectMany(p =>
                    {
                        if (q.Hasta <= p.Desde || q.Desde >= p.Hasta)
                            return new[] { p };
                        var partes = new List<(DateTime, DateTime)>();
                        if (q.Desde > p.Desde) partes.Add((p.Desde, q.Desde));
                        if (q.Hasta < p.Hasta) partes.Add((q.Hasta, p.Hasta));
                        return partes.ToArray();
                    }).ToList();
                }
                resultado.AddRange(piezas);
            }
            return resultado;
        }

        private static double Horas(IEnumerable<(DateTime Desde, DateTime Hasta)> tramos) =>
            tramos.Sum(t => (t.Hasta - t.Desde).TotalHours);
    }
}
