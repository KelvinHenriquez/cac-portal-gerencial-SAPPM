using PortalGerencialBackEnd.Core.Calculo;

namespace PortalGerencialBackEnd.Tests;

public class CalculadoraConfiabilidadTests
{
    private static readonly DateTime Desde = new(2026, 10, 1);
    private static readonly DateTime Hasta = new(2026, 10, 11); // 10 días = 240 h
    private static readonly DateTime Ahora = new(2026, 10, 15);

    [Fact]
    public void Mtbf_cuenta_el_tiempo_en_operacion_de_todos_los_equipos_y_mttr_el_de_restauracion()
    {
        // 3 equipos; 2 fallas de 24 h cada una dentro del período.
        var fallas = new[]
        {
            new FallaEquipo(new DateTime(2026, 10, 2), new DateTime(2026, 10, 3), 24),
            new FallaEquipo(new DateTime(2026, 10, 5), new DateTime(2026, 10, 6), 24),
        };
        var c = CalculadoraConfiabilidad.Calcular(fallas, 3, Desde, Hasta, Ahora);
        Assert.Equal(2, c.Fallas);
        Assert.Equal(24m, c.MttrHoras);
        Assert.Equal((3 * 240m - 48m) / 2, c.MtbfHoras); // 336 h
    }

    [Fact]
    public void Una_falla_que_empezo_antes_resta_solo_su_parte_dentro_del_periodo_y_no_cuenta_como_falla_nueva()
    {
        var fallas = new[]
        {
            new FallaEquipo(new DateTime(2026, 9, 30), new DateTime(2026, 10, 2), 48),   // 24 h dentro
            new FallaEquipo(new DateTime(2026, 10, 4), null, 0),                          // abierta: hasta el fin del período
        };
        var c = CalculadoraConfiabilidad.Calcular(fallas, 1, Desde, Hasta, Ahora);
        Assert.Equal(1, c.Fallas);
        Assert.Equal(48m, c.MttrHoras); // la que terminó dentro del período
        Assert.Equal(240m - 24m - 168m, c.MtbfHoras);
    }

    [Fact]
    public void Sin_fallas_no_hay_mtbf_ni_mttr_y_el_periodo_se_recorta_a_hoy()
    {
        var c = CalculadoraConfiabilidad.Calcular(Array.Empty<FallaEquipo>(), 5, Desde, new DateTime(2026, 12, 31), Ahora);
        Assert.Equal(0, c.Fallas);
        Assert.Null(c.MtbfHoras);
        Assert.Null(c.MttrHoras);

        var unaHoy = new[] { new FallaEquipo(new DateTime(2026, 10, 14), null, 0) };
        var r = CalculadoraConfiabilidad.Calcular(unaHoy, 1, Desde, new DateTime(2026, 12, 31), Ahora);
        Assert.Equal(14 * 24m - 24m, r.MtbfHoras); // período recortado al 15/10
    }
}
