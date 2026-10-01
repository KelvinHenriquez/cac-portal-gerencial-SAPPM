namespace PortalGerencialBackEnd.Core.Settings
{
    public class JwtSettings
    {
        public string Key { get; set; } = default!;
        public string Issuer { get; set; } = default!;
        public string Audience { get; set; } = default!;
        public bool RequireHttpsMetadata { get; set; } = false;
        public int AccessTokenExpirationMinutes { get; set; } = 480; // un turno de trabajo
    }
}
