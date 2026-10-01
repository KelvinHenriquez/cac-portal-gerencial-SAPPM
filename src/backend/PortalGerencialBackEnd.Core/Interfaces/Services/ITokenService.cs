namespace PortalGerencialBackEnd.Core.Interfaces.Services
{
    public interface ITokenService
    {
        (string token, DateTimeOffset expiresAt) GenerateJwtToken(string usuario, string rol);
    }
}
