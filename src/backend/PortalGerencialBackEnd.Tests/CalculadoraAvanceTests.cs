using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Core.Dtos.Estructura;

namespace PortalGerencialBackEnd.Tests;

public class CalculadoraAvanceTests
{
    // Tres semanas lunes-domingo con 20, 50 y 30 actividades (y días distintos).
    private static readonly List<SemanaPlanDto> Plan = new()
    {
        new() { NoSemana = 11, FechaInicio = new(2026, 9, 14), FechaFin = new(2026, 9, 20), Actividad = 20, Dias = 1 },
        new() { NoSemana = 12, FechaInicio = new(2026, 9, 21), FechaFin = new(2026, 9, 27), Actividad = 50, Dias = 1 },
        new() { NoSemana = 13, FechaInicio = new(2026, 9, 28), FechaFin = new(2026, 10, 4), Actividad = 30, Dias = 2 }
    };

    [Fact]
    public void Programado_de_cada_semana_sale_del_plan_por_actividades_o_por_dias()
    {
        var porAct = CalculadoraAvance.Calcular(2, "TR", Plan, BasePlan.Actividades, Array.Empty<OrdenAvance>(), Array.Empty<ActividadAvance>());
        Assert.Equal(new[] { 20m, 50m, 30m }, porAct.Select(s => s.ProgPct));

        var porDias = CalculadoraAvance.Calcular(2, "TR", Plan, BasePlan.Dias, Array.Empty<OrdenAvance>(), Array.Empty<ActividadAvance>());
        Assert.Equal(new[] { 25m, 25m, 50m }, porDias.Select(s => s.ProgPct));
    }

    [Fact]
    public void Real_por_ordenes_es_el_porcentaje_de_ordenes_cerradas_en_cada_semana()
    {
        var ordenes = new[]
        {
            new OrdenAvance("1", new DateTime(2026, 9, 15)),
            new OrdenAvance("2", new DateTime(2026, 9, 29)),
            new OrdenAvance("3", null),
            new OrdenAvance("4", new DateTime(2026, 8, 1))   // antes del programa: cuenta en la primera semana
        };
        var r = CalculadoraAvance.Calcular(2, "TR", Plan, BasePlan.Actividades, ordenes, Array.Empty<ActividadAvance>());

        Assert.Equal(new[] { 50m, 0m, 25m }, r.Select(s => s.RealOrdenesPct));
        Assert.All(r, s => Assert.Equal(4, s.OrdenesTotal));
    }

    [Fact]
    public void Real_por_actividades_pondera_por_dias_y_separa_programado_de_no_programado()
    {
        var actividades = new[]
        {
            new ActividadAvance(2m, new DateTime(2026, 9, 22), Programada: true),
            new ActividadAvance(1m, new DateTime(2026, 9, 23), Programada: false),
            new ActividadAvance(5m, null, Programada: true),
            new ActividadAvance(2m, null, Programada: true)
        };
        var semana12 = CalculadoraAvance.Calcular(2, "TR", Plan, BasePlan.Actividades, Array.Empty<OrdenAvance>(), actividades)[1];

        Assert.Equal(30m, semana12.RealActPct);        // 3 de 10 días
        Assert.Equal(20m, semana12.RealActProgPct);
        Assert.Equal(10m, semana12.RealActNoProgPct);
        Assert.Equal(2, semana12.ActTerminadas);
    }

    [Theory]
    // Filas de la captura del reporte de Oracle (semana 13 = actual):
    //           ProgCierre RealHastaAnterior ProgAct RealActual  -> PendAnt TPendR TotalEjec
    [InlineData(66.67, 56.31, 5.55, 0.00, 10.36, 15.91, 56.31)]   // Cabezales
    [InlineData(54.55, 70.16, 4.55, 0.03, 0.00, 4.52, 70.19)]     // Cosechadoras
    [InlineData(54.80, 50.33, 4.63, 0.21, 4.47, 8.89, 50.54)]     // Vagones
    [InlineData(66.67, 69.78, 5.52, 6.29, 0.00, 0.00, 76.07)]     // Alzadoras
    public void Columnas_a_la_fecha_como_el_reporte_de_Oracle(
        double progCierre, double realAnterior, double progAct, double realActual,
        double pendAnt, double tPendR, double totalEjec)
    {
        var semanas = new List<(DateTime, DateTime, decimal, decimal)>
        {
            (new(2026, 9, 21), new(2026, 9, 27), (decimal)progCierre, (decimal)realAnterior),
            (new(2026, 9, 28), new(2026, 10, 4), (decimal)progAct, (decimal)realActual),
            (new(2026, 10, 5), new(2026, 10, 11), 5m, 0m)
        };
        var c = CalculadoraAvance.ColumnasALaFecha(semanas, new DateTime(2026, 10, 1));

        Assert.Equal((decimal)progCierre, c.ProgCierre);
        Assert.Equal((decimal)progAct, c.ProgAct);
        Assert.Equal((decimal)pendAnt, c.PendAnt);
        Assert.Equal((decimal)tPendR, c.TPendR);
        Assert.Equal((decimal)totalEjec, c.TotalEjec);
        Assert.Equal((decimal)(progCierre + progAct) + 5m, c.TProg);
    }
}
