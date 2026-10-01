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

            try
            {
                await using var connection = new OracleConnection(builder.ConnectionString);
                await connection.OpenAsync();
                return (ResultadoValidacionOracle.Valido, false);
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
                        return (ResultadoValidacionOracle.Valido, true);
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
    }
}
