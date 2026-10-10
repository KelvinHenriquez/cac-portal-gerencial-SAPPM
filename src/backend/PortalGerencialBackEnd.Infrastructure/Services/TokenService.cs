using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Core.Settings;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    public sealed class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;

        public TokenService(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public (string token, DateTimeOffset expiresAt) GenerateJwtToken(string usuario, IEnumerable<string> roles)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, usuario),
                new(ClaimTypes.NameIdentifier, usuario),
                new(ClaimTypes.Name, usuario),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            // Un claim por rol de Oracle: [Authorize(Roles = ...)] y los permisos salen de ahí.
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt.UtcDateTime,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return (tokenHandler.WriteToken(token), expiresAt);
        }
    }
}
