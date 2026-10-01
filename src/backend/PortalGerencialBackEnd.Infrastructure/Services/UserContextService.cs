using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PortalGerencialBackEnd.Core.Interfaces.Common;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    public class UserContextService : IUserContextService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContextService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("No hay HttpContext disponible.");

        public string Usuario => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("El token no contiene un usuario.");

        public string Rol => User.FindFirst(ClaimTypes.Role)?.Value
            ?? throw new InvalidOperationException("El token no contiene un rol.");
    }
}
