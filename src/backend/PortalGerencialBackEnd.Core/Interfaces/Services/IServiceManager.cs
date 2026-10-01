namespace PortalGerencialBackEnd.Core.Interfaces.Services
{
    /// <summary>Agrupa todos los servicios de aplicación — mismo patrón IServiceManager de TPresto.</summary>
    public interface IServiceManager
    {
        IAuthService AuthService { get; }
    }
}
