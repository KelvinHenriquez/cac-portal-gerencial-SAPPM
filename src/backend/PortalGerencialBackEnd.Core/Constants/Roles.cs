namespace PortalGerencialBackEnd.Core.Constants
{
    /// <summary>
    /// Roles de Oracle del portal (decisión del 10/10/2026). Se dan y quitan con GRANT / REVOKE en
    /// Oracle; el portal los lee al iniciar sesión y viajan en el token. Se suman entre sí.
    /// </summary>
    public static class Roles
    {
        /// <summary>TI: todo, incluidos los parámetros técnicos y las actualizaciones manuales.</summary>
        public const string Admin = "ROL_PORTAL_ADMIN";

        /// <summary>Edita los parámetros de negocio (no los técnicos). Se combina con un rol de área.</summary>
        public const string Parametros = "ROL_PORTAL_PARAMETROS";

        /// <summary>Gerencia: consulta las dos áreas.</summary>
        public const string Gerencial = "ROL_PORTAL_GERENCIAL";

        /// <summary>Centro de Servicios: Disponibilidad, Avance y Permanencia de CDS.</summary>
        public const string CentroServicios = "ROL_PORTAL_CDS";

        /// <summary>Industrial: Permanencia de IND (y los reportes industriales que vengan).</summary>
        public const string Industrial = "ROL_PORTAL_IND";

        public static readonly string[] Todos = { Admin, Parametros, Gerencial, CentroServicios, Industrial };

        // Para [Authorize(Roles = ...)]: basta con tener uno de la lista.
        public const string VenCentroServicios = CentroServicios + "," + Gerencial + "," + Admin;
        public const string Actualizan = Admin;
    }

    /// <summary>Códigos de área (los de PERMANENCIA.AREAS).</summary>
    public static class Areas
    {
        public const string CentroServicios = "CDS";
        public const string Industrial = "IND";
    }
}
