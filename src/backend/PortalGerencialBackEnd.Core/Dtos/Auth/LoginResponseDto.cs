namespace PortalGerencialBackEnd.Core.Dtos.Auth
{
    public class LoginResponseDto
    {
        public string AccessToken { get; set; } = default!;
        public DateTimeOffset ExpiresAt { get; set; }
        public string Usuario { get; set; } = default!;
        /// <summary>Roles de Oracle del portal que tiene el usuario (ROL_PORTAL_*).</summary>
        public List<string> Roles { get; set; } = new();
        public PermisosDto Permisos { get; set; } = new();

        /// <summary>Oracle avisa (ORA-28002) que la contraseña está por vencer; se muestra como aviso, no bloquea.</summary>
        public bool PasswordPorVencer { get; set; }
    }
}
