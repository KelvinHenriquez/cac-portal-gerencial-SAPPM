using Microsoft.Extensions.Caching.Memory;
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
    private sealed class ValidadorFijo(ResultadoValidacionOracle resultado) : IOracleCredentialValidator
    {
        public Task<(ResultadoValidacionOracle Resultado, bool PasswordPorVencer)> ValidarAsync(string usuario, string password)
            => Task.FromResult((resultado, false));
    }

    private sealed class TokenFijo : ITokenService
    {
        public (string token, DateTimeOffset expiresAt) GenerateJwtToken(string usuario, string rol) => ("token", DateTimeOffset.UtcNow.AddHours(1));
    }

    private static readonly LoginRequestDto Login = new() { Usuario = "sinacceso1", Password = "clave" };

    private static (AuthService Servicio, LoginAttemptLimiter Limiter) Crear(ResultadoValidacionOracle resultado)
    {
        var limiter = new LoginAttemptLimiter(
            new MemoryCache(new MemoryCacheOptions()),
            new LoginSettings { MaxIntentosFallidos = 5, VentanaMinutos = 15, BloqueoMinutos = 15 },
            () => DateTimeOffset.UtcNow);
        return (new AuthService(new ValidadorFijo(resultado), limiter, new TokenFijo()), limiter);
    }

    [Fact]
    public async Task Sin_el_rol_de_acceso_no_entra_y_lo_dice()
    {
        var (servicio, _) = Crear(ResultadoValidacionOracle.SinRolDeAcceso);
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => servicio.LoginAsync(Login));
        Assert.Contains("no tiene acceso al Portal Gerencial", ex.Message);
    }

    [Fact]
    public async Task Sin_el_rol_no_cuenta_como_intento_fallido()
    {
        var (servicio, limiter) = Crear(ResultadoValidacionOracle.SinRolDeAcceso);
        for (var i = 0; i < 6; i++)
            await Assert.ThrowsAsync<ForbiddenException>(() => servicio.LoginAsync(Login));
        Assert.Null(limiter.BloqueadoHasta("SINACCESO1"));
    }

    [Fact]
    public async Task Con_el_rol_entra()
    {
        var (servicio, _) = Crear(ResultadoValidacionOracle.Valido);
        var r = await servicio.LoginAsync(Login);
        Assert.Equal("SINACCESO1", r.Usuario);
    }
}
