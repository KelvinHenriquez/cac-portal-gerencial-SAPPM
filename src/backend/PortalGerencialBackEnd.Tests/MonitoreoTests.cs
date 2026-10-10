using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Infrastructure.Jobs;

namespace PortalGerencialBackEnd.Tests;

public class MonitoreoTests
{
    private static readonly DateTime Ahora = new(2026, 10, 2, 9, 0, 0);
    private static readonly TimeSpan Cada24h = TimeSpan.FromHours(24);

    [Fact]
    public void Con_datos_al_dia_no_se_alerta()
    {
        Assert.False(ReglaAlertaFrescura.DebeAlertar(false, Ahora.AddHours(-3), null, Ahora, Cada24h));
    }

    [Fact]
    public void Primera_vez_desactualizado_se_alerta()
    {
        Assert.True(ReglaAlertaFrescura.DebeAlertar(true, Ahora.AddHours(-30), null, Ahora, Cada24h));
    }

    [Fact]
    public void No_se_repite_cada_hora_por_el_mismo_problema()
    {
        Assert.False(ReglaAlertaFrescura.DebeAlertar(true, Ahora.AddHours(-30), Ahora.AddHours(-5), Ahora, Cada24h));
    }

    [Fact]
    public void Se_repite_cuando_pasa_el_intervalo()
    {
        Assert.True(ReglaAlertaFrescura.DebeAlertar(true, Ahora.AddHours(-60), Ahora.AddHours(-24), Ahora, Cada24h));
    }

    [Fact]
    public void Un_aviso_de_un_problema_ya_resuelto_no_frena_el_nuevo()
    {
        // Avisó hace 10 h; después hubo una corrida OK (hace 8 h... y ya pasaron las horas de alerta otra vez).
        Assert.True(ReglaAlertaFrescura.DebeAlertar(true, Ahora.AddHours(-8), Ahora.AddHours(-10), Ahora, Cada24h));
    }

    [Fact]
    public void Nunca_hubo_corrida_exitosa_cuenta_como_desactualizado_y_alerta()
    {
        Assert.True(ReglaAlertaFrescura.DebeAlertar(true, null, null, Ahora, Cada24h));
    }

    [Theory]
    [InlineData(4, 59, 2026, 10, 2, 5, 0)]  // antes de la hora: hoy
    [InlineData(5, 0, 2026, 10, 3, 5, 0)]   // justo a la hora: ya pasó, mañana
    [InlineData(23, 0, 2026, 10, 3, 5, 0)]  // de noche: mañana
    public void Proxima_corrida_diaria(int h, int m, int anio, int mes, int dia, int hh, int mm)
    {
        var ahora = new DateTime(2026, 10, 2, h, m, 0);
        Assert.Equal(new DateTime(anio, mes, dia, hh, mm, 0),
            ActualizacionProgramadaService.ProximaCorrida(ahora, new TimeSpan(5, 0, 0)));
    }

    [Theory]
    [InlineData(4, 59, null, false)]            // antes de la hora
    [InlineData(5, 0, null, true)]              // a la hora, sin intento
    [InlineData(14, 0, null, true)]             // el servidor arrancó tarde: corre igual
    [InlineData(5, 30, "2026-10-02 05:00", false)] // falló a las 5: espera la hora
    [InlineData(6, 0, "2026-10-02 05:00", true)]   // reintento una hora después
    [InlineData(5, 0, "2026-10-01 05:00", true)]   // el intento de ayer no cuenta
    public void Toca_intentar_la_corrida_diaria(int h, int m, string? ultimoIntento, bool esperado)
    {
        var ahora = new DateTime(2026, 10, 2, h, m, 0);
        DateTime? intento = ultimoIntento == null ? null : DateTime.Parse(ultimoIntento, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(esperado, ActualizacionProgramadaService.TocaIntentar(ahora, new TimeSpan(5, 0, 0), intento));
    }
}
