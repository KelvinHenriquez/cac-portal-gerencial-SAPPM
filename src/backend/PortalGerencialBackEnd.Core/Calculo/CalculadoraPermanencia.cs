namespace PortalGerencialBackEnd.Core.Calculo
{
    /// <summary>Un cambio de estado de la orden (JCDS): estado de sistema (I0002) o de usuario (E0002).</summary>
    public record CambioEstado(string Codigo, DateTime Fecha, bool Inactivo);

    /// <summary>Códigos de estado de usuario del perfil de las órdenes PM (parámetro PERMANENCIA.ESTADOS_USUARIO).</summary>
    public record EstadosPermanencia(string? EnProceso, string? Finalizada, string? EsperaRepuestos)
    {
        public const string Creada = "I0001";
        public const string Liberada = "I0002";
        public const string CierreTecnico = "I0045";
    }

    /// <summary>
    /// Hitos y tiempos de una orden correctiva. Los pasos van entre hitos consecutivos que existan:
    /// si falta uno (p. ej. nunca pasó por "En proceso"), su tiempo queda dentro del paso siguiente.
    /// </summary>
    public record PermanenciaOrden(
        DateTime Inicio,
        DateTime? Aviso,
        DateTime Creada,
        DateTime? Liberada,
        DateTime? EnProceso,
        DateTime? Finalizada,
        DateTime? CierreTecnico,
        bool Terminada,
        decimal? HorasRespuesta,
        decimal? HorasLiberacion,
        decimal? HorasEsperaInicio,
        decimal? HorasEjecucion,
        decimal? HorasCierreAdministrativo,
        decimal HorasPermanencia,
        decimal HorasEsperaRepuestos);

    /// <summary>
    /// Tiempo de permanencia de un equipo por una orden correctiva (decisión del 06/10/2026): desde
    /// que se abre el aviso (o se crea la orden, si no tiene aviso) hasta que el trabajo queda
    /// FINALIZADO (estado de usuario FINL; si no, fin real; si no, cierre técnico). El cierre técnico
    /// se muestra aparte como "cierre administrativo": a veces se hace en lote, días después.
    /// Tiempos de calendario (24/7).
    /// </summary>
    public static class CalculadoraPermanencia
    {
        public static PermanenciaOrden Calcular(
            DateTime? fechaAviso, DateTime fechaCreacion, DateTime? finReal, DateTime? cierreTecnico,
            IReadOnlyCollection<CambioEstado> historial, EstadosPermanencia estados, DateTime ahora)
        {
            DateTime? PrimeraActivacion(string? codigo) =>
                codigo == null ? null
                : historial.Where(c => !c.Inactivo && c.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase))
                    .Select(c => (DateTime?)c.Fecha).Min();

            var creada = PrimeraActivacion(EstadosPermanencia.Creada) ?? fechaCreacion;
            // Un aviso registrado después de crear la orden no adelanta el inicio.
            var aviso = fechaAviso is { } a && a <= creada ? a : (DateTime?)null;
            var inicio = aviso ?? creada;
            var liberada = Despues(PrimeraActivacion(EstadosPermanencia.Liberada), creada);
            var enProceso = Despues(PrimeraActivacion(estados.EnProceso), liberada ?? creada);
            var cierre = Despues(PrimeraActivacion(EstadosPermanencia.CierreTecnico) ?? cierreTecnico, creada);
            var finalizada = Despues(PrimeraActivacion(estados.Finalizada) ?? finReal ?? cierre, inicio);
            var terminada = finalizada != null;
            var fin = finalizada ?? ahora;

            // Pasos entre hitos consecutivos presentes.
            var hitos = new (DateTime? Fecha, int Paso)[] { (inicio, -1), (creada, 0), (liberada, 1), (enProceso, 2), (finalizada, 3) };
            var horas = new decimal?[4];
            DateTime previo = inicio;
            foreach (var (fecha, paso) in hitos.Skip(1))
            {
                if (fecha is not { } f) continue;
                horas[paso] = Horas(previo, f);
                previo = f;
            }

            return new PermanenciaOrden(
                Inicio: inicio,
                Aviso: aviso,
                Creada: creada,
                Liberada: liberada,
                EnProceso: enProceso,
                Finalizada: finalizada,
                CierreTecnico: cierre,
                Terminada: terminada,
                HorasRespuesta: aviso != null ? horas[0] : null,
                HorasLiberacion: horas[1],
                HorasEsperaInicio: horas[2],
                HorasEjecucion: horas[3],
                HorasCierreAdministrativo: finalizada != null && cierre != null ? Horas(finalizada.Value, Max(cierre.Value, finalizada.Value)) : null,
                HorasPermanencia: Horas(inicio, fin),
                HorasEsperaRepuestos: TiempoEnEstado(historial, estados.EsperaRepuestos, inicio, fin));
        }

        /// <summary>Horas en que un estado estuvo activo dentro de [desde, hasta] (puede entrar y salir varias veces).</summary>
        public static decimal TiempoEnEstado(IReadOnlyCollection<CambioEstado> historial, string? codigo, DateTime desde, DateTime hasta)
        {
            if (codigo == null) return 0m;
            var total = 0m;
            DateTime? activo = null;
            foreach (var c in historial.Where(c => c.Codigo.Equals(codigo, StringComparison.OrdinalIgnoreCase)).OrderBy(c => c.Fecha))
            {
                if (!c.Inactivo && activo == null) activo = c.Fecha;
                else if (c.Inactivo && activo != null)
                {
                    total += Solape(activo.Value, c.Fecha, desde, hasta);
                    activo = null;
                }
            }
            if (activo != null) total += Solape(activo.Value, hasta, desde, hasta);
            return total;
        }

        /// <summary>Promedio y mediana de una lista de horas.</summary>
        public static (decimal Promedio, decimal Mediana) Estadisticos(IReadOnlyCollection<decimal> horas)
        {
            if (horas.Count == 0) return (0m, 0m);
            var orden = horas.OrderBy(h => h).ToList();
            var medio = orden.Count / 2;
            var mediana = orden.Count % 2 == 1 ? orden[medio] : (orden[medio - 1] + orden[medio]) / 2m;
            return (Math.Round(horas.Average(), 2), Math.Round(mediana, 2));
        }

        private static DateTime? Despues(DateTime? fecha, DateTime? limite) =>
            fecha is { } f && limite is { } l && f < l ? l : fecha;

        private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;

        private static decimal Horas(DateTime desde, DateTime hasta) =>
            hasta <= desde ? 0m : Math.Round((decimal)(hasta - desde).TotalHours, 2);

        private static decimal Solape(DateTime a, DateTime b, DateTime desde, DateTime hasta)
        {
            var ini = a > desde ? a : desde;
            var fin = b < hasta ? b : hasta;
            return Horas(ini, fin);
        }
    }
}
