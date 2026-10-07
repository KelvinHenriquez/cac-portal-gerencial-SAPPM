using PortalGerencialBackEnd.Core.Calculo;

namespace PortalGerencialBackEnd.Tests;

public class CalculadoraPermanenciaTests
{
    private static readonly EstadosPermanencia Estados = new("E0002", "E0003", "E0009");
    private static readonly DateTime Ahora = new(2026, 10, 6, 12, 0, 0);
    private static DateTime D(int dia, int hora, int mes = 9) => new(2026, mes, dia, hora, 0, 0);

    [Fact]
    public void Mide_cada_paso_desde_el_aviso_hasta_finalizada()
    {
        var historial = new[]
        {
            new CambioEstado("I0001", D(1, 10), false),
            new CambioEstado("I0002", D(1, 14), false),
            new CambioEstado("E0002", D(2, 8), false),
            new CambioEstado("E0002", D(4, 8), true),
            new CambioEstado("E0003", D(4, 8), false),
            new CambioEstado("I0045", D(10, 8), false)
        };

        var r = CalculadoraPermanencia.Calcular(D(1, 7), D(1, 10), null, null, historial, Estados, Ahora);

        Assert.True(r.Terminada);
        Assert.Equal(3m, r.HorasRespuesta);          // aviso 07:00 -> orden 10:00
        Assert.Equal(4m, r.HorasLiberacion);         // 10:00 -> 14:00
        Assert.Equal(18m, r.HorasEsperaInicio);      // 14:00 -> 08:00 del día 2
        Assert.Equal(48m, r.HorasEjecucion);         // 2 días en proceso
        Assert.Equal(73m, r.HorasPermanencia);       // aviso -> finalizada
        Assert.Equal(144m, r.HorasCierreAdministrativo); // finalizada -> cierre técnico 6 días después
    }

    [Fact]
    public void Sin_aviso_empieza_en_la_orden_y_sin_FINL_termina_en_el_fin_real()
    {
        var historial = new[] { new CambioEstado("I0001", D(1, 10), false), new CambioEstado("I0002", D(1, 10), false) };

        var r = CalculadoraPermanencia.Calcular(null, D(1, 10), finReal: D(3, 10), cierreTecnico: D(3, 12), historial, Estados, Ahora);

        Assert.Null(r.HorasRespuesta);
        Assert.Null(r.HorasEsperaInicio);            // nunca pasó por "En proceso"
        Assert.Equal(48m, r.HorasEjecucion);         // liberada -> fin real, absorbe el paso que falta
        Assert.Equal(48m, r.HorasPermanencia);
        Assert.Equal(2m, r.HorasCierreAdministrativo);
    }

    [Fact]
    public void Orden_abierta_cuenta_su_antiguedad_a_hoy_y_la_espera_de_repuestos()
    {
        var historial = new[]
        {
            new CambioEstado("I0001", D(30, 6), false),
            new CambioEstado("E0009", D(1, 6, 10), false),   // entra a espera de repuestos el 01/10
            new CambioEstado("E0009", D(3, 6, 10), true),    // sale el 03/10
            new CambioEstado("E0009", D(5, 12, 10), false)   // vuelve a entrar el 05/10 y sigue
        };

        var r = CalculadoraPermanencia.Calcular(null, D(30, 6), null, null, historial, Estados, Ahora);

        Assert.False(r.Terminada);
        Assert.Equal(150m, r.HorasPermanencia);      // 30/09 06:00 -> 06/10 12:00
        Assert.Equal(72m, r.HorasEsperaRepuestos);   // 48 h + 24 h
    }

    [Fact]
    public void Promedio_y_mediana()
    {
        var (prom, med) = CalculadoraPermanencia.Estadisticos(new[] { 10m, 20m, 30m, 400m });
        Assert.Equal(115m, prom);
        Assert.Equal(25m, med);
    }
}
