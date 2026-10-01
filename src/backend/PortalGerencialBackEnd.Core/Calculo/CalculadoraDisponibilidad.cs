using System.Globalization;
using PortalGerencialBackEnd.Core.Entities.Calculo;

namespace PortalGerencialBackEnd.Core.Calculo
{
    /// <summary>Una orden ya cruzada con Oracle: se conoce el código legado de su equipo.</summary>
    public record OrdenDisponibilidad(string CodigoLegado, string EstadoOrden, DateTime? FechaInicioReal, DateTime? FechaFinReal);

    /// <summary>
    /// Disponibilidad diaria derivada de las fechas de las órdenes (sección 6.1) y su agregado
    /// semanal. Lógica pura, sin base de datos: el proceso de actualización le pasa los datos.
    /// </summary>
    public static class CalculadoraDisponibilidad
    {
        /// <summary>
        /// Días en que una orden deja a su equipo fuera de servicio, o null si no lo deja:
        /// - con fecha fin real: de inicio real a fin real, sin importar el estado actual. Así una
        ///   reparación ya cerrada sigue contando como indisponibilidad en los días que duró (si se
        ///   mirara solo el estado actual, como sugiere la sección 6.1, el pasado cambiaría cada vez
        ///   que se cierra una orden).
        /// - sin fecha fin real: de inicio real a hoy, solo si el estado es de "fuera de servicio".
        /// - sin inicio real: la orden todavía no detuvo al equipo.
        /// </summary>
        public static (DateTime Desde, DateTime Hasta)? VentanaFueraServicio(
            OrdenDisponibilidad orden, ISet<string> estadosFueraServicio, DateTime hoy)
        {
            if (orden.FechaInicioReal is not { } inicio)
                return null;

            if (orden.FechaFinReal is { } fin)
                return fin.Date < inicio.Date ? null : (inicio.Date, fin.Date);

            return estadosFueraServicio.Contains(orden.EstadoOrden) ? (inicio.Date, hoy.Date) : null;
        }

        /// <param name="equiposActivosPorFamilia">Familia → códigos legados de sus equipos activos.</param>
        /// <param name="requeridos">(TipoReporte, Familia) → suma de CANT_REQUERIDA.</param>
        public static List<CalcDisponibilidadDiaria> CalcularDiaria(
            IReadOnlyDictionary<string, IReadOnlyCollection<string>> equiposActivosPorFamilia,
            IReadOnlyDictionary<(string TipoReporte, string Familia), int> requeridos,
            IEnumerable<OrdenDisponibilidad> ordenes,
            ISet<string> estadosFueraServicio,
            DateTime desde,
            DateTime hasta,
            DateTime hoy)
        {
            // Equipo → ventanas en que estuvo fuera de servicio.
            var ventanasPorEquipo = ordenes
                .Select(o => (o.CodigoLegado, Ventana: VentanaFueraServicio(o, estadosFueraServicio, hoy)))
                .Where(x => x.Ventana != null)
                .GroupBy(x => x.CodigoLegado)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Ventana!.Value).ToList());

            var resultado = new List<CalcDisponibilidadDiaria>();
            foreach (var ((tipoReporte, familia), requerido) in requeridos)
            {
                var equipos = equiposActivosPorFamilia.TryGetValue(familia, out var lista)
                    ? lista
                    : Array.Empty<string>();

                for (var dia = desde.Date; dia <= hasta.Date; dia = dia.AddDays(1))
                {
                    var fueraServicio = equipos.Count(e =>
                        ventanasPorEquipo.TryGetValue(e, out var ventanas)
                        && ventanas.Any(v => v.Desde <= dia && dia <= v.Hasta));

                    var disponible = equipos.Count - fueraServicio;
                    resultado.Add(new CalcDisponibilidadDiaria
                    {
                        TipoReporte = tipoReporte,
                        FamiliaCodigo = familia,
                        Fecha = dia,
                        CantEquipo = equipos.Count,
                        EqRequerido = requerido,
                        EqFueraServicio = fueraServicio,
                        EqDisponible = disponible,
                        DispRealPct = Porcentaje(disponible, requerido),
                        DispPotencialPct = Porcentaje(disponible, equipos.Count)
                    });
                }
            }

            return resultado;
        }

        /// <summary>Agrega días a semanas ISO (lunes a domingo) por tipo de reporte y familia.</summary>
        public static List<CalcDisponibilidadSemanal> AgregarSemanal(IEnumerable<CalcDisponibilidadDiaria> diaria)
        {
            return diaria
                .GroupBy(d => (d.TipoReporte, d.FamiliaCodigo, Anio: ISOWeek.GetYear(d.Fecha), Semana: ISOWeek.GetWeekOfYear(d.Fecha)))
                .Select(g =>
                {
                    var ultimoDia = g.OrderBy(d => d.Fecha).Last();
                    var lunes = ISOWeek.ToDateTime(g.Key.Anio, g.Key.Semana, DayOfWeek.Monday);
                    return new CalcDisponibilidadSemanal
                    {
                        TipoReporte = g.Key.TipoReporte,
                        FamiliaCodigo = g.Key.FamiliaCodigo,
                        AnioIso = g.Key.Anio,
                        SemanaIso = g.Key.Semana,
                        FechaInicioSemana = lunes,
                        FechaFinSemana = lunes.AddDays(6),
                        DiasCalculados = g.Count(),
                        CantEquipo = ultimoDia.CantEquipo,
                        EqRequerido = ultimoDia.EqRequerido,
                        EqDisponibleProm = Math.Round((decimal)g.Average(d => d.EqDisponible), 2),
                        EqDisponibleMin = g.Min(d => d.EqDisponible),
                        DispRealPct = Math.Round(g.Average(d => d.DispRealPct), 2),
                        DispRealMinPct = g.Min(d => d.DispRealPct),
                        DispPotencialPct = Math.Round(g.Average(d => d.DispPotencialPct), 2)
                    };
                })
                .ToList();
        }

        /// <summary>
        /// Parte / total en %, topado en 100 (decisión del 30/09/2026). Sin requeridos (total 0) la
        /// familia no puede estar en falta: 100.
        /// </summary>
        public static decimal Porcentaje(int parte, int total) =>
            total <= 0 ? 100m : Math.Min(100m, Math.Round(100m * parte / total, 2));

        /// <summary>
        /// Disponibilidad de un grupo de familias: total disponible / total requerido, contando de
        /// cada familia como máximo sus requeridos. Así 11 cosechadoras para 10 requeridas no tapan
        /// 2 camiones que faltan (decisión del 30/09/2026: "menos engañoso").
        /// </summary>
        public static decimal PorcentajeTotal(IEnumerable<(decimal Disponibles, int Requeridos)> familias)
        {
            var lista = familias.ToList();
            var requeridos = lista.Sum(f => f.Requeridos);
            if (requeridos <= 0)
                return 100m;
            var cubiertos = lista.Sum(f => Math.Min(f.Disponibles, f.Requeridos));
            return Math.Min(100m, Math.Round(100m * cubiertos / requeridos, 2));
        }
    }
}
