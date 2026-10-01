using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Core.Entities.Calculo;

namespace PortalGerencialBackEnd.Tests;

public class CalculadoraDisponibilidadTests
{
    private static readonly HashSet<string> FueraServicio = new(StringComparer.OrdinalIgnoreCase) { "Liberado", "En Proceso" };
    private static readonly DateTime Hoy = new(2026, 10, 1);

    private static List<CalcDisponibilidadDiaria> Calcular(
        IEnumerable<OrdenDisponibilidad> ordenes, int requerido = 4, int equipos = 4,
        DateTime? desde = null, DateTime? hasta = null)
    {
        var activos = new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["TRACTORES"] = Enumerable.Range(1, equipos).Select(i => $"TR-{i:D3}").ToList()
        };
        var requeridos = new Dictionary<(string, string), int> { [("CS-P30", "TRACTORES")] = requerido };
        return CalculadoraDisponibilidad.CalcularDiaria(
            activos, requeridos, ordenes, FueraServicio, desde ?? Hoy.AddDays(-9), hasta ?? Hoy, Hoy);
    }

    [Fact]
    public void Orden_cerrada_cuenta_en_los_dias_que_duro_aunque_hoy_este_cerrada()
    {
        var orden = new OrdenDisponibilidad("TR-001", "Cerrado tecnicamente", new DateTime(2026, 9, 24), new DateTime(2026, 9, 26));
        var dias = Calcular(new[] { orden }).ToDictionary(d => d.Fecha);

        Assert.Equal(0, dias[new DateTime(2026, 9, 23)].EqFueraServicio);
        Assert.Equal(1, dias[new DateTime(2026, 9, 24)].EqFueraServicio);
        Assert.Equal(1, dias[new DateTime(2026, 9, 26)].EqFueraServicio);
        Assert.Equal(0, dias[new DateTime(2026, 9, 27)].EqFueraServicio);
    }

    [Fact]
    public void Orden_abierta_en_estado_fuera_de_servicio_cuenta_hasta_hoy()
    {
        var orden = new OrdenDisponibilidad("TR-001", "En Proceso", new DateTime(2026, 9, 28), null);
        var dias = Calcular(new[] { orden });

        Assert.All(dias.Where(d => d.Fecha >= new DateTime(2026, 9, 28)), d => Assert.Equal(1, d.EqFueraServicio));
        Assert.All(dias.Where(d => d.Fecha < new DateTime(2026, 9, 28)), d => Assert.Equal(0, d.EqFueraServicio));
    }

    [Fact]
    public void Orden_sin_inicio_real_o_abierta_en_otro_estado_no_cuenta()
    {
        var ordenes = new[]
        {
            new OrdenDisponibilidad("TR-001", "Liberado", null, null),
            new OrdenDisponibilidad("TR-002", "Pendiente", new DateTime(2026, 9, 28), null)
        };
        Assert.All(Calcular(ordenes), d => Assert.Equal(0, d.EqFueraServicio));
    }

    [Fact]
    public void Dos_ordenes_del_mismo_equipo_el_mismo_dia_cuentan_un_solo_equipo()
    {
        var ordenes = new[]
        {
            new OrdenDisponibilidad("TR-001", "En Proceso", new DateTime(2026, 9, 30), null),
            new OrdenDisponibilidad("TR-001", "Cerrado tecnicamente", new DateTime(2026, 9, 29), new DateTime(2026, 10, 1))
        };
        var hoy = Calcular(ordenes).Single(d => d.Fecha == Hoy);

        Assert.Equal(1, hoy.EqFueraServicio);
        Assert.Equal(3, hoy.EqDisponible);
    }

    [Fact]
    public void La_disponibilidad_se_topa_en_100()
    {
        // 4 equipos disponibles para 2 requeridos = 200%, se muestra 100%.
        var dia = Calcular(Array.Empty<OrdenDisponibilidad>(), requerido: 2).First();
        Assert.Equal(100m, dia.DispRealPct);
        Assert.Equal(100m, dia.DispPotencialPct);
    }

    [Fact]
    public void Porcentajes_real_y_potencial()
    {
        var ordenes = new[] { new OrdenDisponibilidad("TR-001", "En Proceso", Hoy, null) };
        var hoy = Calcular(ordenes, requerido: 4, equipos: 4).Single(d => d.Fecha == Hoy);

        Assert.Equal(75m, hoy.DispRealPct);      // 3 de 4 requeridos
        Assert.Equal(75m, hoy.DispPotencialPct); // 3 de 4 equipos
    }

    [Fact]
    public void Sin_requeridos_la_familia_no_esta_en_falta()
    {
        Assert.Equal(100m, CalculadoraDisponibilidad.Porcentaje(0, 0));
    }

    [Fact]
    public void Agregado_semanal_promedia_y_guarda_el_peor_dia()
    {
        // Lunes 21 a domingo 27 de septiembre de 2026 (semana ISO 39), TR-001 fuera el 24 y 25.
        var orden = new OrdenDisponibilidad("TR-001", "Cerrado tecnicamente", new DateTime(2026, 9, 24), new DateTime(2026, 9, 25));
        var diaria = Calcular(new[] { orden }, desde: new DateTime(2026, 9, 21), hasta: new DateTime(2026, 9, 27));

        var semana = Assert.Single(CalculadoraDisponibilidad.AgregarSemanal(diaria));
        Assert.Equal(39, semana.SemanaIso);
        Assert.Equal(new DateTime(2026, 9, 21), semana.FechaInicioSemana);
        Assert.Equal(7, semana.DiasCalculados);
        Assert.Equal(75m, semana.DispRealMinPct);
        Assert.Equal(Math.Round((5 * 100m + 2 * 75m) / 7, 2), semana.DispRealPct);
        Assert.Equal(3, semana.EqDisponibleMin);
    }
}
