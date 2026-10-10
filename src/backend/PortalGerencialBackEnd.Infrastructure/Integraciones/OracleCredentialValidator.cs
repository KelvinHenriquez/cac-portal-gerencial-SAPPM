using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Core.Settings;

namespace PortalGerencialBackEnd.Infrastructure.Integraciones
{
    /// <summary>
    /// Sección 4.3: valida usuario/clave abriendo una conexión real contra Oracle. Es la misma clase
    /// en local (Oracle Free en Docker, con usuarios nombrados de prueba) y en producción — solo
    /// cambia Oracle:DataSourceLogin.
    /// </summary>
    public class OracleCredentialValidator : IOracleCredentialValidator
    {
        private readonly OracleSettings _settings;
        private readonly ILogger<OracleCredentialValidator> _logger;

        public OracleCredentialValidator(IOptions<OracleSettings> settings, ILogger<OracleCredentialValidator> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<(ResultadoValidacionOracle Resultado, bool PasswordPorVencer, IReadOnlyList<string> Roles)> ValidarAsync(string usuario, string password)
        {
            // El builder escapa los valores: un usuario con ';' o '=' no puede inyectar otras claves
            // en la cadena de conexión (p. ej. otro Data Source).
            var builder = new OracleConnectionStringBuilder
            {
                UserID = usuario,
                Password = password,
                DataSource = _settings.DataSourceLogin,
                // Sin pool: cada login es una prueba de credenciales aislada, y la conexión de un
                // usuario nombrado no debe quedar abierta en el pool del servidor.
                Pooling = false,
                ConnectionTimeout = 10
            };

            await using var connection = new OracleConnection(builder.ConnectionString);
            try
            {
                await connection.OpenAsync();
                var roles = await RolesDelPortalAsync(connection);
                return roles.Count == 0
                    ? (ResultadoValidacionOracle.SinRolDeAcceso, false, roles)
                    : (ResultadoValidacionOracle.Valido, await ClavePorVencerAsync(connection), roles);
            }
            catch (OracleException ex)
            {
                switch (ex.Number)
                {
                    case 1017: // usuario/clave inválidos
                    case 1005: // clave nula
                        return (ResultadoValidacionOracle.CredencialesInvalidas, false, Array.Empty<string>());
                    case 28000: // cuenta bloqueada
                        return (ResultadoValidacionOracle.CuentaBloqueada, false, Array.Empty<string>());
                    case 28001: // clave vencida
                        return (ResultadoValidacionOracle.PasswordVencido, false, Array.Empty<string>());
                    case 28002: // la clave vence en N días: la conexión sí se abre
                    {
                        // El driver avisa la expiración por excepción; si la sesión quedó abierta se ven los roles.
                        if (connection.State != System.Data.ConnectionState.Open)
                        {
                            _logger.LogWarning("No se pudieron leer los roles de un usuario con la clave por vencer.");
                            return (ResultadoValidacionOracle.SinRolDeAcceso, true, Array.Empty<string>());
                        }
                        var rolesGracia = await RolesDelPortalAsync(connection);
                        return (rolesGracia.Count == 0 ? ResultadoValidacionOracle.SinRolDeAcceso : ResultadoValidacionOracle.Valido, true, rolesGracia);
                    }
                    default:
                        _logger.LogError(ex, "Oracle no respondió al validar credenciales (ORA-{Numero}).", ex.Number);
                        return (ResultadoValidacionOracle.OracleNoDisponible, false, Array.Empty<string>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo contactar Oracle para validar credenciales.");
                return (ResultadoValidacionOracle.OracleNoDisponible, false, Array.Empty<string>());
            }
        }

        /// <summary>
        /// Roles del portal (ROL_PORTAL_*) que tiene el usuario. USER_ROLE_PRIVS ve los dados directo
        /// (aunque no sean por defecto); SESSION_ROLES, los habilitados, incluidos los que vienen
        /// dentro de otro rol.
        /// </summary>
        private static async Task<IReadOnlyList<string>> RolesDelPortalAsync(OracleConnection connection)
        {
            await using var cmd = connection.CreateCommand();
            cmd.BindByName = true;
            var nombres = Roles.Todos.Select((r, i) => (Param: $":r{i}", Rol: r)).ToList();
            cmd.CommandText =
                "SELECT DISTINCT ROL FROM (SELECT GRANTED_ROLE AS ROL FROM USER_ROLE_PRIVS UNION SELECT ROLE FROM SESSION_ROLES) " +
                $"WHERE ROL IN ({string.Join(", ", nombres.Select(n => n.Param))})";
            foreach (var n in nombres)
                cmd.Parameters.Add(new OracleParameter(n.Param.TrimStart(':'), n.Rol));
            var roles = new List<string>();
            await using var lector = await cmd.ExecuteReaderAsync();
            while (await lector.ReadAsync())
                roles.Add(lector.GetString(0));
            return roles;
        }

        /// <summary>
        /// ODP.NET administrado abre la conexión sin lanzar ORA-28002 cuando la clave está en período
        /// de gracia: se pregunta a Oracle. También avisa si vence en los próximos 7 días.
        /// </summary>
        private async Task<bool> ClavePorVencerAsync(OracleConnection connection)
        {
            try
            {
                await using var cmd = connection.CreateCommand();
                cmd.CommandText =
                    "SELECT CASE WHEN ACCOUNT_STATUS LIKE '%GRACE%' OR EXPIRY_DATE < SYSDATE + 7 THEN 1 ELSE 0 END FROM USER_USERS";
                return Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 1;
            }
            catch (OracleException ex)
            {
                // Es solo un aviso: si no se puede leer, se entra igual.
                _logger.LogWarning("No se pudo leer el vencimiento de la clave (ORA-{Numero}).", ex.Number);
                return false;
            }
        }
    }
}
