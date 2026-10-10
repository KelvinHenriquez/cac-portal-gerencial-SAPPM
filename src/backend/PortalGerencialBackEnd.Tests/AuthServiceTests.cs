using Microsoft.Extensions.Caching.Memory;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Dtos.Auth;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Core.Settings;
using PortalGerencialBackEnd.Infrastructure.Seguridad;
using PortalGerencialBackEnd.Infrastructure.Services;

namespace PortalGerencialBackEnd.Tests;

public class AuthServiceTests
{
    private sealed class ValidadorFijo(ResultadoValidacionOracle resultado, params string[] roles) : IOracleCredentialValidator
    {
        public Task<(ResultadoValidacionOracle Resultado, bool PasswordPorVencer, IReadOnlyList<string> Roles)> ValidarAsync(string usuario, string password)
            => Task.FromResult((resultado, false, (IReadOnlyList<string>)roles));
    }

    private sealed class TokenFijo : ITokenService
    {
        public List<string> Roles { get; } = new();

        public (string token, DateTimeOffset expiresAt) GenerateJwtToken(string usuario, IEnumerable<string> roles)
        {
            Roles.AddRange(roles);
            return ("token", DateTimeOffset.UtcNow.AddHours(1));
        }
    }

    private static readonly LoginRequestDto Login = new() { Usuario = "sinacceso1", Password = "clave" };

    private static (AuthService Servicio, LoginAttemptLimiter Limiter, TokenFijo Token) Crear(ResultadoValidacionOracle resultado, params string[] roles)
    {
        var limiter = new LoginAttemptLimiter(
            new MemoryCache(new MemoryCacheOptions()),
            new LoginSettings { MaxIntentosFallidos = 5, VentanaMinutos = 15, BloqueoMinutos = 15 },
            () => DateTimeOffset.UtcNow);
        var token = new TokenFijo();
        return (new AuthService(new ValidadorFijo(resultado, roles), limiter, token), limiter, token);
    }

    [Fact]
    public async Task Sin_ningun_rol_del_portal_no_entra_y_lo_dice()
    {
        var (servicio, _, _) = Crear(ResultadoValidacionOracle.SinRolDeAcceso);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => servicio.LoginAsync(Login));
        Assert.Contains("no tiene acceso al Portal Gerencial", ex.Message);
    }

    [Fact]
    public async Task Sin_el_rol_no_cuenta_como_intento_fallido()
    {
        var (servicio, limiter, _) = Crear(ResultadoValidacionOracle.SinRolDeAcceso);
        for (var i = 0; i < 6; i++)
            await Assert.ThrowsAsync<ForbiddenException>(() => servicio.LoginAsync(Login));
        Assert.Null(limiter.BloqueadoHasta("SINACCESO1"));
    }

    [Fact]
    public async Task Con_un_rol_entra_y_sus_roles_viajan_en_el_token_y_en_la_respuesta()
    {
        var (servicio, _, token) = Crear(ResultadoValidacionOracle.Valido, Roles.CentroServicios, Roles.Parametros);
        var r = await servicio.LoginAsync(Login);
        Assert.Equal("SINACCESO1", r.Usuario);
        Assert.Equal(new[] { Roles.CentroServicios, Roles.Parametros }, token.Roles);
        Assert.Equal(new[] { "CDS" }, r.Permisos.Areas);
        Assert.True(r.Permisos.EditarParametros);
        Assert.False(r.Permisos.Actualizar);
    }
}
