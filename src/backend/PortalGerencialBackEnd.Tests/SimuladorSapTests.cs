using PortalGerencialBackEnd.Infrastructure.Integraciones.Local;

namespace PortalGerencialBackEnd.Tests;

public class SimuladorSapTests
{
    private static readonly List<SimuladorSap.EquipoOracle> Equipos =
        Enumerable.Range(1, 40)
            .Select(i => new SimuladorSap.EquipoOracle($"TR-{i:D3}", $"Tractor #{i}", "TRACTORES", Activo: i != 40))
            .ToList();

    private static readonly List<SimuladorSap.FamiliaPrograma> Familias = new()
    {
        new("TRACTORES", new DateTime(2026, 6, 1), new DateTime(2026, 11, 27))
    };

    [Fact]
    public void Mismo_dia_mismos_datos()
    {
        var a = SimuladorSap.Generar(Equipos, Familias, new DateTime(2026, 10, 1));
        var b = SimuladorSap.Generar(Equipos, Familias, new DateTime(2026, 10, 1));

        Assert.Equal(
            a.Ordenes.Select(o => (o.OrdenSapId, o.EstadoOrden, o.FechaInicioReal, o.FechaFinReal)),
            b.Ordenes.Select(o => (o.OrdenSapId, o.EstadoOrden, o.FechaInicioReal, o.FechaFinReal)));
    }

    [Fact]
    public void Las_ordenes_avanzan_con_los_dias_sin_cambiar_de_numero()
    {
        var antes = SimuladorSap.Generar(Equipos, Familias, new DateTime(2026, 8, 1)).Ordenes.ToDictionary(o => o.OrdenSapId);
        var despues = SimuladorSap.Generar(Equipos, Familias, new DateTime(2026, 12, 31)).Ordenes.ToDictionary(o => o.OrdenSapId);

        // Todo lo que existía antes sigue existiendo con el mismo número y la misma fecha de inicio real.
        Assert.All(antes.Values, o =>
        {
            Assert.True(despues.ContainsKey(o.OrdenSapId));
            if (o.FechaInicioReal != null)
                Assert.Equal(o.FechaInicioReal, despues[o.OrdenSapId].FechaInicioReal);
        });

        // Al cierre del programa ya no queda ninguna reparación pendiente o liberada sin empezar.
        Assert.DoesNotContain(despues.Values, o => o.ClaseOrden == "ZC03" && o.FechaInicioReal == null);
    }

    [Fact]
    public void No_genera_nada_con_fecha_futura()
    {
        var hoy = new DateTime(2026, 10, 1);
        var datos = SimuladorSap.Generar(Equipos, Familias, hoy);

        Assert.All(datos.Ordenes, o =>
        {
            Assert.True(o.FechaCreacion <= hoy);
            Assert.True(o.FechaInicioReal == null || o.FechaInicioReal <= hoy);
            Assert.True(o.FechaFinReal == null || o.FechaFinReal <= hoy);
            Assert.True(o.FechaModificacion <= hoy);
        });
        Assert.All(datos.Historial, h => Assert.True(h.FechaHoraCambio.Date <= hoy));
    }

    [Fact]
    public void Equipo_inactivo_no_tiene_ordenes_y_hay_equipos_solo_de_sap()
    {
        var datos = SimuladorSap.Generar(Equipos, Familias, new DateTime(2026, 10, 1));
        var tr040 = datos.Equipos.Single(e => e.CodigoLegado == "TR-040");

        Assert.DoesNotContain(datos.Ordenes, o => o.EquipoSapId == tr040.EquipoSapId);
        Assert.Contains(datos.Equipos, e => e.CodigoLegado == null);
        Assert.Contains(datos.Equipos, e => e.CodigoLegado == "TR-900");
    }

    [Fact]
    public void Cada_orden_tiene_horas_y_las_cerradas_su_estado_en_el_historial()
    {
        var datos = SimuladorSap.Generar(Equipos, Familias, new DateTime(2026, 10, 1));
        var conHoras = datos.Operaciones.Select(o => o.Operacion.OrdenSapId).ToHashSet();

        Assert.All(datos.Ordenes, o => Assert.Contains(o.OrdenSapId, conHoras));
        Assert.All(datos.Ordenes.Where(o => o.EstadoOrden == SimuladorSap.CerradoTecnicamente), o =>
            Assert.Contains(datos.Historial, h => h.OrdenSapId == o.OrdenSapId && h.EstadoCodigo == "I0045"));
    }
}
