using System.Text.RegularExpressions;
using PortalGerencialBackEnd.Core.Dtos.Auth;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Infrastructure.Seguridad;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        // Nombre de usuario Oracle no entrecomillado: empieza con letra; letras, dígitos, _ $ #.
        private static readonly Regex FormatoUsuarioOracle = new(@"^[A-Z][A-Z0-9_$#]{0,127}$", RegexOptions.Compiled);

        private readonly IOracleCredentialValidator _validator;
        private readonly LoginAttemptLimiter _limiter;
        private readonly ITokenService _tokenService;

        public AuthService(IOracleCredentialValidator validator, LoginAttemptLimiter limiter, ITokenService tokenService)
        {
            _validator = validator;
            _limiter = limiter;
            _tokenService = tokenService;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // Oracle no distingue mayúsculas en usuarios no entrecomillados; la clave sí.
            var usuario = (request.Usuario ?? string.Empty).Trim().ToUpperInvariant();
            var password = request.Password ?? string.Empty;

            if (!FormatoUsuarioOracle.IsMatch(usuario) || password.Length == 0)
                throw new BadRequestException("Usuario o contraseña incorrectos.");

            var bloqueadoHasta = _limiter.BloqueadoHasta(usuario);
            if (bloqueadoHasta != null)
                throw new TooManyRequestsException(MensajeBloqueo(bloqueadoHasta.Value));

            var (resultado, porVencer, roles) = await _validator.ValidarAsync(usuario, password);

            switch (resultado)
            {
                case ResultadoValidacionOracle.Valido:
                    _limiter.RegistrarExito(usuario);
                    var (token, expiresAt) = _tokenService.GenerateJwtToken(usuario, roles);
                    return new LoginResponseDto
                    {
                        AccessToken = token,
                        ExpiresAt = expiresAt,
                        Usuario = usuario,
                        Roles = roles.ToList(),
                        Permisos = PermisosDto.De(roles),
                        PasswordPorVencer = porVencer
                    };

                case ResultadoValidacionOracle.CredencialesInvalidas:
                    var restantes = _limiter.RegistrarFallo(usuario);
                    if (restantes == 0)
                        throw new TooManyRequestsException(MensajeBloqueo(_limiter.BloqueadoHasta(usuario)!.Value));
                    throw new BadRequestException(restantes <= 2
                        ? $"Usuario o contraseña incorrectos. Le quedan {restantes} intento(s) antes de que el acceso se bloquee temporalmente."
                        : "Usuario o contraseña incorrectos.");

                case ResultadoValidacionOracle.CuentaBloqueada:
                    throw new ForbiddenException("Su usuario está bloqueado en Oracle. Contacte al DBA para desbloquearlo.");

                case ResultadoValidacionOracle.SinRolDeAcceso:
                    // Clave correcta: no cuenta como intento fallido.
                    throw new ForbiddenException("Su usuario de Oracle no tiene acceso al Portal Gerencial. Solicítelo al administrador del portal.");

                case ResultadoValidacionOracle.PasswordVencido:
                    throw new ForbiddenException("Su contraseña de Oracle venció. Pida al DBA que la restablezca y vuelva a intentar.");

                default:
                    throw new ServiceUnavailableException("No se pudo contactar la base de datos Oracle. Intente de nuevo en unos minutos.");
            }
        }

        private static string MensajeBloqueo(DateTimeOffset hasta)
        {
            var minutos = Math.Max(1, (int)Math.Ceiling((hasta - DateTimeOffset.UtcNow).TotalMinutes));
            return $"Demasiados intentos fallidos. Por seguridad, espere {minutos} minuto(s) antes de volver a intentar.";
        }
    }
}
