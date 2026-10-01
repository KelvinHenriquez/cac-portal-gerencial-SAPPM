namespace PortalGerencialBackEnd.Core.Settings
{
    /// <summary>Sección "Oracle" de appsettings.</summary>
    public class OracleSettings
    {
        /// <summary>
        /// Data Source (EZ Connect host:puerto/servicio o descriptor completo) contra el que se
        /// validan las credenciales de los usuarios nombrados en el login (sección 4.3).
        /// </summary>
        public string DataSourceLogin { get; set; } = default!;

        /// <summary>
        /// Esquema donde viven los objetos _pgpm. Vacío = el del usuario técnico que conecta. Si en
        /// producción el usuario técnico no es el dueño del esquema, se fija aquí y cada conexión
        /// hace ALTER SESSION SET CURRENT_SCHEMA.
        /// </summary>
        public string? Esquema { get; set; }

        /// <summary>
        /// Versión de SQL que genera el provider de EF (19, 21 o 23; no tiene opción para 11g).
        /// Producción es 11.2.0.4: se deja en 19 y el código evita lo que solo existe desde 12c
        /// (IDENTITY, Take/First traducidos a FETCH FIRST, nombres de más de 30 caracteres). El
        /// stack local corre Oracle 11g XE para que cualquier desliz falle en desarrollo.
        /// </summary>
        public int VersionSql { get; set; } = 19;
    }
}
