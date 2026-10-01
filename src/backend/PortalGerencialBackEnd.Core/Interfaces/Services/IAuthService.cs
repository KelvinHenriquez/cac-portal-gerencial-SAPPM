using PortalGerencialBackEnd.Core.Dtos.Auth;

namespace PortalGerencialBackEnd.Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request);
    }
}
