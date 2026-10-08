using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
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

        public async Task<(ResultadoValidacionOracle Resultado, bool PasswordPorVencer)> ValidarAsync(string usuario, string password)
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
                var resultado = await TieneRolDeAccesoAsync(connection) ? ResultadoValidacionOracle.Valido : ResultadoValidacionOracle.SinRolDeAcceso;
                return (resultado, resultado == ResultadoValidacionOracle.Valido && await ClavePorVencerAsync(connection));
            }
            catch (OracleException ex)
            {
                switch (ex.Number)
                {
                    case 1017: // usuario/clave inválidos
                    case 1005: // clave nula
                        return (ResultadoValidacionOracle.CredencialesInvalidas, false);
                    case 28000: // cuenta bloqueada
                        return (ResultadoValidacionOracle.CuentaBloqueada, false);
                    case 28001: // clave vencida
                        return (ResultadoValidacionOracle.PasswordVencido, false);
                    case 28002: // la clave vence en N días: la conexión sí se abre
                        return (await RolConClavePorVencerAsync(connection), true);
                    default:
                        _logger.LogError(ex, "Oracle no respondió al validar credenciales (ORA-{Numero}).", ex.Number);
                        return (ResultadoValidacionOracle.OracleNoDisponible, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo contactar Oracle para validar credenciales.");
                return (ResultadoValidacionOracle.OracleNoDisponible, false);
            }
        }

        /// <summary>
        /// ¿El usuario tiene el rol de acceso? USER_ROLE_PRIVS ve los roles dados directo (aunque no
        /// sean por defecto); SESSION_ROLES, los habilitados, incluidos los que vienen dentro de otro rol.
        /// </summary>
        private async Task<bool> TieneRolDeAccesoAsync(OracleConnection connection)
        {
            if (string.IsNullOrWhiteSpace(_settings.RolAcceso))
                return true;
            await using var cmd = connection.CreateCommand();
            cmd.BindByName = true;
            cmd.CommandText =
                "SELECT COUNT(*) FROM (SELECT GRANTED_ROLE AS ROL FROM USER_ROLE_PRIVS UNION SELECT ROLE FROM SESSION_ROLES) WHERE ROL = :rol";
            cmd.Parameters.Add(new OracleParameter("rol", _settings.RolAcceso.Trim().ToUpperInvariant()));
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
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

        /// <summary>
        /// Con ORA-28002 el driver avisa por excepción pero la sesión queda abierta: se ve el rol en
        /// ella. Si no quedara abierta, no hay cómo verificarlo y no se deja pasar.
        /// </summary>
        private async Task<ResultadoValidacionOracle> RolConClavePorVencerAsync(OracleConnection connection)
        {
            if (string.IsNullOrWhiteSpace(_settings.RolAcceso))
                return ResultadoValidacionOracle.Valido;
            if (connection.State != System.Data.ConnectionState.Open)
            {
                _logger.LogWarning("No se pudo verificar el rol de acceso de un usuario con la clave por vencer.");
                return ResultadoValidacionOracle.SinRolDeAcceso;
            }
            return await TieneRolDeAccesoAsync(connection) ? ResultadoValidacionOracle.Valido : ResultadoValidacionOracle.SinRolDeAcceso;
        }
    }
}
