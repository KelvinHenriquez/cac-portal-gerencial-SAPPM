namespace PortalGerencialBackEnd.Core.Dtos.Auth
{
    /// <summary>Usuario y contraseña de Oracle (sección 4.3) — el portal no tiene usuarios propios.</summary>
    public class LoginRequestDto
    {
        public string Usuario { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
