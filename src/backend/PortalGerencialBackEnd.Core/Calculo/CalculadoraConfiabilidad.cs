namespace PortalGerencialBackEnd.Core.Calculo
{
    /// <summary>Una correctiva vista como falla: desde el aviso (o la creación) hasta la orden finalizada.</summary>
    public record FallaEquipo(DateTime Inicio, DateTime? Finalizada, decimal HorasRestauracion);

    /// <summary>Resultado para un conjunto de equipos (área, familia, asignación o un equipo) en un período.</summary>
    public record Confiabilidad(int Equipos, int Fallas, decimal? MttrHoras, decimal? MtbfHoras);

    /// <summary>
    /// MTTR y MTBF (pedido del Taller, 14/10/2026), con la misma definición en Disponibilidad y en
    /// Permanencia:
    ///  - MTTR (tiempo medio de restauración) = promedio, de las correctivas finalizadas en el
    ///    período, del tiempo desde el aviso (o la creación) hasta la orden finalizada. Es lo que antes
    ///    se llamaba "permanencia promedio".
    ///  - MTBF (tiempo medio entre fallas) = tiempo en operación / número de fallas: (equipos × horas
    ///    del período − horas fuera de servicio por correctivas) / correctivas que empezaron en el
    ///    período. Cuenta también los equipos que no fallaron (por eso recibe cuántos equipos hay).
    /// Lógica pura, sin base de datos.
    /// </summary>
    public static class CalculadoraConfiabilidad
    {
        /// <param name="desde">Inicio del período (inclusive).</param>
        /// <param name="hasta">Fin del período (exclusivo); se recorta a <paramref name="ahora"/>.</param>
        public static Confiabilidad Calcular(IReadOnlyCollection<FallaEquipo> fallas, int equipos, DateTime desde, DateTime hasta, DateTime ahora)
        {
            if (hasta > ahora) hasta = ahora;
            if (hasta <= desde || equipos <= 0)
                return new Confiabilidad(Math.Max(equipos, 0), 0, null, null);

            var enPeriodo = fallas.Count(f => f.Inicio >= desde && f.Inicio < hasta);
            var restauradas = fallas.Where(f => f.Finalizada is { } fin && fin >= desde && fin < hasta).ToList();
            decimal? mttr = restauradas.Count == 0 ? null : Math.Round(restauradas.Average(f => f.HorasRestauracion), 2);

            // Horas fuera de servicio dentro del período (también de fallas que empezaron antes).
            var horasFuera = fallas.Sum(f =>
            {
                var ini = f.Inicio < desde ? desde : f.Inicio;
                var fin = f.Finalizada ?? ahora;
                if (fin > hasta) fin = hasta;
                return fin > ini ? (decimal)(fin - ini).TotalHours : 0m;
            });
            var horasOperacion = equipos * (decimal)(hasta - desde).TotalHours - horasFuera;
            decimal? mtbf = enPeriodo == 0 ? null : Math.Round(Math.Max(horasOperacion, 0) / enPeriodo, 2);
            return new Confiabilidad(equipos, enPeriodo, mttr, mtbf);
        }
    }
}
