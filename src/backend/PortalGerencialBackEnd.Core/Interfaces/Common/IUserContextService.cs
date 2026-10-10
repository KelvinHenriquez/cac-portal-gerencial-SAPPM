using PortalGerencialBackEnd.Core.Dtos.Auth;

namespace PortalGerencialBackEnd.Core.Interfaces.Common
{
    /// <summary>
    /// Extrae los claims del JWT del usuario autenticado (mismo patrón que TPresto). El usuario es el
    /// nombre de usuario Oracle en mayúsculas — no hay un Id interno porque no hay tabla de usuarios.
    /// </summary>
    public interface IUserContextService
    {
        string Usuario { get; }
        /// <summary>Roles de Oracle del portal (ROL_PORTAL_*) con que inició sesión.</summary>
        IReadOnlyList<string> Roles { get; }
        PermisosDto Permisos { get; }
    }
}
