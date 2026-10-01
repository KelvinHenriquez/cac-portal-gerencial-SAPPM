using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Core.Dtos.Estructura;

namespace PortalGerencialBackEnd.Tests;

public class CalculadoraAvanceTests
{
    // Tres semanas de lunes a domingo: 20% + 50% + 30%.
    private static readonly List<CurvaSemanaDto> Curva = new()
    {
        Semana(1, new DateTime(2026, 9, 14), 20),
        Semana(2, new DateTime(2026, 9, 21), 50),
        Semana(3, new DateTime(2026, 9, 28), 30)
    };

    private static CurvaSemanaDto Semana(int numero, DateTime lunes, decimal pct) => new()
    {
        AnioPrograma = "2026",
        FamiliaCodigo = "TRACTORES",
        NoSemana = numero,
        FechaInicioSemana = lunes,
        FechaFinSemana = lunes.AddDays(6),
        ActividadPct = pct
    };

    [Fact]
    public void El_programado_es_la_curva_acumulada()
    {
        var r = CalculadoraAvance.Calcular("2026", "TRACTORES", Curva, Array.Empty<OrdenAvance>(),
            FormulaAvanceReal.HhOrdenesCerradas, new DateTime(2026, 10, 1));

        Assert.Equal(new[] { 20m, 70m, 100m }, r.Select(x => x.ProgPct));
        Assert.All(r, x => Assert.Equal(0m, x.RealPct));
    }

    [Fact]
    public void El_real_pondera_por_horas_planificadas_de_las_ordenes_cerradas_a_cada_corte()
    {
        var ordenes = new[]
        {
            new OrdenAvance(new DateTime(2026, 9, 18), true, HhPlan: 10, HhReal: 12),  // cerrada semana 1
            new OrdenAvance(new DateTime(2026, 9, 25), true, HhPlan: 30, HhReal: 20),  // cerrada semana 2
            new OrdenAvance(null, false, HhPlan: 60, HhReal: 15)                       // abierta
        };

        var r = CalculadoraAvance.Calcular("2026", "TRACTORES", Curva, ordenes,
            FormulaAvanceReal.HhOrdenesCerradas, hoy: new DateTime(2026, 10, 1));

        Assert.Equal(new[] { 10m, 40m, 40m }, r.Select(x => x.RealPct));
        Assert.Equal(new[] { 10m, 30m, 60m }, r.Select(x => x.PendPct));
        Assert.Equal(new[] { 1, 2, 2 }, r.Select(x => x.OrdenesCerradas));
        Assert.All(r, x => Assert.Equal(100m, x.HhPlan));
    }

    [Fact]
    public void Formula_por_cantidad_de_ordenes()
    {
        var ordenes = new[]
        {
            new OrdenAvance(new DateTime(2026, 9, 18), true, 10, 10),
            new OrdenAvance(null, false, 90, 0)
        };

        var r = CalculadoraAvance.Calcular("2026", "TRACTORES", Curva, ordenes,
            FormulaAvanceReal.OrdenesCerradas, new DateTime(2026, 10, 1));

        Assert.Equal(50m, r[0].RealPct);
    }

    [Fact]
    public void Sin_horas_planificadas_cae_a_contar_ordenes()
    {
        var ordenes = new[]
        {
            new OrdenAvance(new DateTime(2026, 9, 18), true, 0, 0),
            new OrdenAvance(null, false, 0, 0)
        };

        var r = CalculadoraAvance.Calcular("2026", "TRACTORES", Curva, ordenes,
            FormulaAvanceReal.HhOrdenesCerradas, new DateTime(2026, 10, 1));

        Assert.Equal(50m, r[0].RealPct);
    }

    [Fact]
    public void Una_orden_con_fecha_fin_pero_que_no_esta_cerrada_no_suma()
    {
        // Terminó en el taller pero nadie la cerró técnicamente en SAP.
        var ordenes = new[] { new OrdenAvance(new DateTime(2026, 9, 18), false, 10, 10) };

        var r = CalculadoraAvance.Calcular("2026", "TRACTORES", Curva, ordenes,
            FormulaAvanceReal.HhOrdenesCerradas, new DateTime(2026, 10, 1));

        Assert.All(r, x => Assert.Equal(0m, x.RealPct));
    }

    [Fact]
    public void Semanas_futuras_toman_el_real_a_hoy()
    {
        var ordenes = new[]
        {
            new OrdenAvance(new DateTime(2026, 9, 16), true, 50, 50),
            new OrdenAvance(new DateTime(2026, 10, 2), true, 50, 50) // cierra mañana
        };

        var r = CalculadoraAvance.Calcular("2026", "TRACTORES", Curva, ordenes,
            FormulaAvanceReal.HhOrdenesCerradas, hoy: new DateTime(2026, 10, 1));

        Assert.Equal(50m, r[2].RealPct);
    }
}
