using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Dtos.ResumenAvance;
using PortalGerencialBackEnd.Infrastructure.Services;

namespace PortalGerencialBackEnd.Tests;

/// <summary>Resumen de avance de reparación (14/10/2026): fila General y configuración sugerida.</summary>
public class ResumenAvanceTests
{
    private static FilaResumenDto Fila(int cantidad, decimal? progSemana, decimal progAcum, decimal ejecAcum, int programados, int entregados) => new()
    {
        Nombre = "x",
        FamiliaCodigo = "x",
        Cantidad = cantidad,
        ProgSemanaPct = progSemana,
        EjecSemanaPct = 0,
        ProgAcumPct = progAcum,
        EjecAcumPct = ejecAcum,
        DiferenciaPct = ejecAcum - progAcum,
        ProgramadosEntregar = programados,
        Entregados = entregados
    };

    [Fact]
    public void General_PonderaPorCantidad_NoSumaPorcentajes()
    {
        // 10 máquinas al 100 % y 30 al 0 %: el General es 25 %, no 100 % (ni la suma).
        var general = ResumenAvanceService.General("Agrícola", new[]
        {
            Fila(10, 10, 100, 50, 10, 5),
            Fila(30, null, 0, 0, 0, 0),
        });

        Assert.Equal("General Agrícola", general.Nombre);
        Assert.Equal(40, general.Cantidad);
        Assert.Equal(25, general.ProgAcumPct);
        Assert.Equal(12.5m, general.EjecAcumPct);
        Assert.Equal(-12.5m, general.DiferenciaPct);
        // La semana solo pondera las familias con plan esa semana.
        Assert.Equal(10, general.ProgSemanaPct);
        Assert.Equal(10, general.ProgramadosEntregar);
        Assert.Equal(5, general.Entregados);
    }

    private static ConfigAvanceDto Programa(int id, string nombre, params (string Codigo, string Nombre)[] familias) => new()
    {
        ReporteId = id,
        Nombre = nombre,
        Familias = familias.Select((f, i) => new FamiliaAvanceDto { Orden = i + 1, FamiliaCodigo = f.Codigo, FamiliaNombre = f.Nombre }).ToList()
    };

    [Fact]
    public void Sugerida_AgrupaComoLaLamina_YSeparaTractoresPorPrograma()
    {
        var configs = new List<ConfigAvanceDto>
        {
            Programa(27, "Programa De Reparacion 2026", ("1", "Cabezales"), ("2", "Alzadoras"), ("3", "Tractores"), ("4", "Vagones"), ("5", "Cosechadoras")),
            Programa(28, "Programa De Reparacion 2026 Produccion", ("76-1", "Tractores")),
            Programa(29, "Programa De Reparacion 2026 Cosecha", ("76-1", "Tractores")),
            Programa(30, "Programa De Reparacion 2026 Varios", ("76-1", "Tractores")),
        };

        var filas = ResumenAvanceService.Sugerida(configs);

        Assert.Equal(new[] { "Alzadoras", "Tractores varios", "Tractores de Cosecha", "Tractores de Producción", "Cosechadoras", "Cabezales", "Vagones" },
            filas.Select(f => f.Nombre));
        Assert.Equal(new[] { 27, 30, 29, 28, 27, 27, 27 }, filas.Select(f => f.ReporteId));
        Assert.Equal("Maquinaria Agrícola", filas[0].Grupo);
        Assert.Equal("Maquinaria Automotriz", filas[5].Grupo);
        Assert.Equal("Maquinaria Ferroviaria", filas[6].Grupo);
        Assert.Equal(Enumerable.Range(1, 7), filas.Select(f => f.Orden));
        Assert.DoesNotContain(filas, f => f.ReporteId == 27 && f.FamiliaCodigo == "3");
    }
}
