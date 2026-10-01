using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Infrastructure.Seguridad;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    public class ServiceManager : IServiceManager
    {
        private readonly Lazy<IAuthService> _authService;

        public ServiceManager(
            IOracleCredentialValidator oracleCredentialValidator,
            LoginAttemptLimiter loginAttemptLimiter,
            ITokenService tokenService)
        {
            _authService = new Lazy<IAuthService>(() =>
                new AuthService(oracleCredentialValidator, loginAttemptLimiter, tokenService));
        }

        public IAuthService AuthService => _authService.Value;
    }
}
