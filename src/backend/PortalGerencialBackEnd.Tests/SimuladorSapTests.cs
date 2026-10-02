using PortalGerencialBackEnd.Infrastructure.Integraciones.Local;

namespace PortalGerencialBackEnd.Tests;

public class SimuladorSapTests
{
    private static readonly List<SimuladorSap.EquipoOracle> Equipos =
        Enumerable.Range(1, 30)
            .Select(i => new SimuladorSap.EquipoOracle($"TR-{i:D3}", $"Tractor #{i}", "TRACTORES", Activo: i != 30))
            .ToList();

    private static readonly List<SimuladorSap.ProgramaFamilia> Programas = new()
    {
        new(2, "TRACTORES", new DateTime(2026, 6, 1), new DateTime(2026, 11, 27)),
        new(1, "TRACTORES", new DateTime(2025, 6, 30), new DateTime(2025, 11, 28)) // antes de SAP: sin órdenes
    };

    private static readonly DateTime Hoy = new(2026, 10, 1, 17, 0, 0);

    [Fact]
    public void Mismo_instante_mismos_datos()
    {
        var a = SimuladorSap.Generar(Equipos, Programas, Hoy);
        var b = SimuladorSap.Generar(Equipos, Programas, Hoy);
        Assert.Equal(a.Ordenes.Select(o => (o.OrdenSapId, o.EstadoOrden, o.FechaCierre)), b.Ordenes.Select(o => (o.OrdenSapId, o.EstadoOrden, o.FechaCierre)));
        Assert.Equal(a.Operaciones.Count, b.Operaciones.Count);
    }

    [Fact]
    public void Las_ordenes_avanzan_con_los_dias_sin_cambiar_de_numero_ni_de_creacion()
    {
        var antes = SimuladorSap.Generar(Equipos, Programas, new DateTime(2026, 8, 15)).Ordenes.ToDictionary(o => o.OrdenSapId);
        var despues = SimuladorSap.Generar(Equipos, Programas, new DateTime(2026, 12, 31)).Ordenes.ToDictionary(o => o.OrdenSapId);

        Assert.All(antes.Values, o =>
        {
            Assert.True(despues.ContainsKey(o.OrdenSapId), o.OrdenSapId);
            Assert.Equal(o.FechaCreacion, despues[o.OrdenSapId].FechaCreacion);
        });
        Assert.Contains(despues.Values, o => o.ClaseOrden == "ZC03" && o.EstadoOrden == SimuladorSap.Cerrada);
    }

    [Fact]
    public void Nada_ocurre_en_el_futuro_ni_antes_de_la_salida_en_vivo_de_SAP()
    {
        var datos = SimuladorSap.Generar(Equipos, Programas, Hoy);
        Assert.All(datos.Ordenes, o =>
        {
            Assert.InRange(o.FechaCreacion, SimuladorSap.InicioSap, Hoy);
            Assert.True(o.FechaCierre == null || o.FechaCierre <= Hoy);
        });
        Assert.All(datos.Operaciones, op => Assert.True(op.FechaUltimaConf == null || op.FechaUltimaConf <= Hoy));
        Assert.DoesNotContain(datos.Ordenes, o => o.ClaseOrden == "ZC03" && o.OrdenSapId.EndsWith("010")); // programa 2025 (ReporteId 1)
    }

    [Fact]
    public void Hay_las_tres_clases_de_orden_y_las_correctivas_traen_aviso()
    {
        var datos = SimuladorSap.Generar(Equipos, Programas, Hoy);
        Assert.Contains(datos.Ordenes, o => o.ClaseOrden == "ZC01");
        Assert.Contains(datos.Ordenes, o => o.ClaseOrden == "ZC02");
        Assert.Contains(datos.Ordenes, o => o.ClaseOrden == "ZC03");
        var avisos = datos.Avisos.ToDictionary(a => a.AvisoSapId);
        Assert.All(datos.Ordenes.Where(o => o.ClaseOrden == "ZC01"), o => Assert.True(avisos.ContainsKey(o.AvisoSapId!)));
        Assert.Contains(datos.Avisos, a => a.Parada);
        Assert.Contains(datos.Avisos, a => !a.Parada);
    }

    [Fact]
    public void Las_reparaciones_tienen_actividades_programadas_y_agregadas_con_sistema()
    {
        var datos = SimuladorSap.Generar(Equipos, Programas, Hoy);
        var zc03 = datos.Ordenes.Where(o => o.ClaseOrden == "ZC03").Select(o => o.OrdenSapId).ToHashSet();
        var ops = datos.Operaciones.Where(op => zc03.Contains(op.OrdenSapId)).ToList();

        Assert.All(ops, op => Assert.False(string.IsNullOrEmpty(op.Sistema)));
        Assert.Contains(ops, op => op.Programada);
        Assert.Contains(ops, op => !op.Programada);
        Assert.All(ops.Where(op => op.TrabajoRealHh > 0), op => Assert.NotNull(op.FechaUltimaConf));
    }

    [Fact]
    public void Equipo_inactivo_no_tiene_ordenes_y_hay_equipos_solo_de_sap()
    {
        var datos = SimuladorSap.Generar(Equipos, Programas, Hoy);
        var tr030 = datos.Equipos.Single(e => e.CodigoLegado == "TR-030");
        Assert.DoesNotContain(datos.Ordenes, o => o.EquipoSapId == tr030.EquipoSapId);
        Assert.Contains(datos.Equipos, e => e.CodigoLegado == null);
        Assert.Contains(datos.Equipos, e => e.CodigoLegado == "TR-900");
    }

    [Fact]
    public void El_programa_solo_genera_reparaciones_para_los_equipos_de_sus_OT_asignadas()
    {
        // La misma familia en un reporte con solo tres de sus equipos (como Tractores en Nivelación).
        var programas = new List<SimuladorSap.ProgramaFamilia>
        {
            new(3, "TRACTORES", new DateTime(2026, 6, 1), new DateTime(2026, 11, 27), new HashSet<string> { "TR-001", "TR-002", "TR-003" })
        };
        var datos = SimuladorSap.Generar(Equipos, programas, new DateTime(2026, 12, 31));
        var legados = datos.Equipos.ToDictionary(e => e.EquipoSapId, e => e.CodigoLegado);

        var conReparacion = datos.Ordenes.Where(o => o.ClaseOrden == "ZC03").Select(o => legados[o.EquipoSapId!]).ToHashSet();
        Assert.Equal(new HashSet<string?> { "TR-001", "TR-002", "TR-003" }, conReparacion);
    }
}
