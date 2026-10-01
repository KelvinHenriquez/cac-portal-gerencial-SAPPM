using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PortalGerencialBackEnd.API.Extensions;
using PortalGerencialBackEnd.Core.Dtos.Auth;
using PortalGerencialBackEnd.Core.Interfaces.Common;
using PortalGerencialBackEnd.Core.Interfaces.Services;

namespace PortalGerencialBackEnd.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        private readonly IUserContextService _userContext;

        public AuthController(IServiceManager serviceManager, IUserContextService userContext)
        {
            _serviceManager = serviceManager;
            _userContext = userContext;
        }

        /// <summary>Login con usuario/clave de Oracle (sección 4.3).</summary>
        [HttpPost("login")]
        [EnableRateLimiting(ApplicationServicesExtensions.PoliticaLogin)]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto request)
        {
            var result = await _serviceManager.AuthService.LoginAsync(request);
            return Ok(result);
        }

        /// <summary>Usuario de la sesión actual — permite al frontend confirmar que el token sigue vigente.</summary>
        [HttpGet("me")]
        [Authorize]
        public ActionResult<object> Me() => Ok(new { usuario = _userContext.Usuario, rol = _userContext.Rol });
    }
}
