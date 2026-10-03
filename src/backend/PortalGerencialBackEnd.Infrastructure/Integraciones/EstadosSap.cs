namespace PortalGerencialBackEnd.Infrastructure.Integraciones
{
    /// <summary>
    /// Conversión de lo que entrega SAP a lo que usa el portal. La comparten el importador de
    /// Excel (Sprint 0) y el cliente RFC, para que las dos fuentes lleguen al mismo staging.
    /// </summary>
    internal static class EstadosSap
    {
        public const string Creada = "Creada";
        public const string Liberada = "Liberada";
        public const string EnProceso = "En Proceso";
        public const string Cerrada = "Cerrada";
        public const string Anulada = "Anulada";

        /// <summary>Estado de sistema de "cierre técnico" (TECO): fin de la indisponibilidad.</summary>
        public const string CodigoCierreTecnico = "I0045";

        /// <summary>Estado de sistema "creada" (CRTD): su alta en JCDS trae la hora de creación.</summary>
        public const string CodigoCreada = "I0001";

        /// <summary>
        /// Estado del portal a partir del status de sistema (STTXT, en español o inglés) y, si
        /// hace falta, el de usuario: "LIB. NOTP" → En Proceso, "CTEC" → Cerrada, "PTBO" → Anulada.
        /// </summary>
        public static string Derivar(string? statusSistema, string? statusUsuario = null)
        {
            var s = Tokens(statusSistema);
            if (s.Overlaps(new[] { "PTBO", "BORR", "DLFL", "DLT", "ANUL" })) return Anulada;
            if (s.Overlaps(new[] { "CTEC", "CERR", "TECO", "CLSD" })) return Cerrada;
            if (s.Overlaps(new[] { "NOTP", "NOTI", "PCNF", "CNF" })) return EnProceso;
            if ((statusUsuario ?? "").Contains("PROCES", StringComparison.OrdinalIgnoreCase)) return EnProceso;
            if (s.Overlaps(new[] { "LIB.", "LIB", "LIBE", "REL" })) return Liberada;
            return Creada;
        }

        /// <summary>Equipo inactivo en SAP: status INAC o petición/marca de borrado.</summary>
        public static bool EquipoInactivo(string? status) =>
            Tokens(status).Overlaps(new[] { "INAC", "INACTIVO", "PTBO", "BORR", "DLFL", "INACT" });

        /// <summary>Número SAP sin ceros a la izquierda (AUFNR, EQUNR, QMNUM): igual en Excel y RFC.</summary>
        public static string? Id(string? valor)
        {
            var v = valor?.Trim();
            if (string.IsNullOrEmpty(v)) return null;
            var sinCeros = v.All(char.IsDigit) ? v.TrimStart('0') : v;
            return sinCeros.Length == 0 ? "0" : sinCeros;
        }

        private static HashSet<string> Tokens(string? texto) =>
            (texto ?? "").ToUpperInvariant()
                .Split(new[] { ' ', ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();
    }
}
