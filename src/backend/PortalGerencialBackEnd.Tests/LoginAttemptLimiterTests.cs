using Microsoft.Extensions.Caching.Memory;
using PortalGerencialBackEnd.Core.Settings;
using PortalGerencialBackEnd.Infrastructure.Seguridad;

namespace PortalGerencialBackEnd.Tests;

public class LoginAttemptLimiterTests
{
    private DateTimeOffset _ahora = new(2026, 9, 30, 10, 0, 0, TimeSpan.Zero);

    private LoginAttemptLimiter Crear() => new(
        new MemoryCache(new MemoryCacheOptions()),
        new LoginSettings { MaxIntentosFallidos = 5, VentanaMinutos = 15, BloqueoMinutos = 15 },
        () => _ahora);

    [Fact]
    public void Bloquea_al_llegar_al_maximo_de_fallos()
    {
        var limiter = Crear();
        for (var i = 0; i < 4; i++) limiter.RegistrarFallo("GERENTE1");
        Assert.Null(limiter.BloqueadoHasta("GERENTE1"));

        Assert.Equal(0, limiter.RegistrarFallo("GERENTE1"));
        Assert.Equal(_ahora.AddMinutes(15), limiter.BloqueadoHasta("GERENTE1"));
    }

    [Fact]
    public void El_bloqueo_expira()
    {
        var limiter = Crear();
        for (var i = 0; i < 5; i++) limiter.RegistrarFallo("GERENTE1");

        _ahora = _ahora.AddMinutes(16);
        Assert.Null(limiter.BloqueadoHasta("GERENTE1"));
        // Tras el bloqueo, el contador vuelve a empezar.
        Assert.Equal(4, limiter.RegistrarFallo("GERENTE1"));
    }

    [Fact]
    public void Los_fallos_viejos_fuera_de_la_ventana_no_cuentan()
    {
        var limiter = Crear();
        for (var i = 0; i < 4; i++) limiter.RegistrarFallo("GERENTE1");

        _ahora = _ahora.AddMinutes(16);
        Assert.Equal(4, limiter.RegistrarFallo("GERENTE1"));
    }

    [Fact]
    public void Un_login_exitoso_reinicia_el_contador()
    {
        var limiter = Crear();
        for (var i = 0; i < 4; i++) limiter.RegistrarFallo("GERENTE1");
        limiter.RegistrarExito("GERENTE1");

        Assert.Equal(4, limiter.RegistrarFallo("GERENTE1"));
    }

    [Fact]
    public void Los_usuarios_se_cuentan_por_separado()
    {
        var limiter = Crear();
        for (var i = 0; i < 5; i++) limiter.RegistrarFallo("GERENTE1");

        Assert.NotNull(limiter.BloqueadoHasta("GERENTE1"));
        Assert.Null(limiter.BloqueadoHasta("GERENTE2"));
    }
}
