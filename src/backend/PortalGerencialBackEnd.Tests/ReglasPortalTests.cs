using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Entities.Sap;
using PortalGerencialBackEnd.Infrastructure.Services;

namespace PortalGerencialBackEnd.Tests;

public class ReglasPortalTests
{
    private static AsignacionDto Asig(int id, string nombre, bool grupo, params string[] equipos) =>
        new() { AsignacionId = id, Orden = id, Nombre = nombre, Grupo = grupo, Equipos = equipos.ToList() };

    [Fact]
    public void Miembro_de_grupo_se_detecta_por_sus_equipos_no_por_la_posicion()
    {
        var asignaciones = new List<AsignacionDto>
        {
            Asig(1, "Aguacatico Goteo", true, "F01", "F02", "F03"),
            Asig(2, "Bomba Goteo 1", false, "F01"),          // debajo del grupo y dentro de él
            Asig(3, "Pozo Finca Madre", false, "F09"),       // debajo del grupo pero independiente
            Asig(4, "Bombas 2 y 3", false, "F02", "F03"),    // lejos del grupo pero dentro de él
            Asig(5, "Mixta", false, "F03", "F10")            // solo una parte en el grupo: cuenta aparte
        };

        var miembros = ReglasPortal.MiembrosDeGrupos(asignaciones);

        Assert.Equal(new[] { 2, 4 }, miembros.Keys.OrderBy(k => k));
        Assert.Equal("Aguacatico Goteo", miembros[4]);
    }

    [Fact]
    public void Actividad_es_del_plan_si_la_creo_un_usuario_de_hoja_de_ruta()
    {
        var orden = new StgSapOrden
        {
            OrdenSapId = "4000001",
            Operaciones =
            {
                new StgSapOperacion { Operacion = "0010", UsuarioCreacion = "PLANIFICADOR", Programada = false },
                new StgSapOperacion { Operacion = "0020", UsuarioCreacion = "jperez", Programada = true },
                new StgSapOperacion { Operacion = "0030", UsuarioCreacion = null, Programada = false }
            }
        };

        ReglasPortal.AplicarHojaRuta(new[] { orden }, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "planificador" });

        Assert.Equal(new[] { true, false, false }, orden.Operaciones.Select(o => o.Programada));
    }

    [Fact]
    public void Sin_usuarios_configurados_queda_la_marca_de_SAP()
    {
        var orden = new StgSapOrden { OrdenSapId = "4000002", Operaciones = { new StgSapOperacion { Operacion = "0010", UsuarioCreacion = "X", Programada = true } } };

        ReglasPortal.AplicarHojaRuta(new[] { orden }, new HashSet<string>());

        Assert.True(orden.Operaciones.Single().Programada);
    }

    [Theory]
    [InlineData("F01168", "012-052002-0023", "F01168")]   // con Ficha: la Ficha
    [InlineData(null, "011-050101-0001", "011-050101-0001")] // equipo de fábrica sin Ficha: el código SAP
    [InlineData("  ", "10000001", "10000001")]
    public void Clave_de_equipo_es_la_Ficha_o_el_codigo_SAP(string? ficha, string equipoSap, string esperado)
    {
        Assert.Equal(esperado, MapeoEquipos.Clave(ficha, equipoSap));
    }

    [Fact]
    public void El_plan_de_una_familia_cubre_todo_el_calendario_del_programa()
    {
        static SemanaPlanDto Sem(int n, decimal act) => new()
        {
            NoSemana = n, FechaInicio = new DateTime(2026, 7, 6).AddDays(7 * (n - 1)), FechaFin = new DateTime(2026, 7, 12).AddDays(7 * (n - 1)), Actividad = act
        };
        var corta = new FamiliaAvanceDto { FamiliaCodigo = "1-1", Semanas = { Sem(1, 10), Sem(2, 5) } };
        var larga = new FamiliaAvanceDto { FamiliaCodigo = "2-2", Semanas = { Sem(1, 1), Sem(2, 1), Sem(3, 1), Sem(4, 1) } };
        var reporte = new ConfigAvanceDto { Familias = { corta, larga } };

        var plan = ReglasPortal.PlanEnCalendario(reporte, corta);

        Assert.Equal(new[] { 1, 2, 3, 4 }, plan.Select(s => s.NoSemana));
        Assert.Equal(new[] { 10m, 5m, 0m, 0m }, plan.Select(s => s.Actividad));
        Assert.Equal(new DateTime(2026, 7, 27), plan[3].FechaInicio);
    }

    [Fact]
    public void Orden_terminada_por_fin_real_o_por_cierre_tecnico()
    {
        var orden = new StgSapOrden
        {
            OrdenSapId = "230000000229", EstadoOrden = "Cerrada",
            FechaFinReal = new DateTime(2026, 7, 17), FechaCierre = new DateTime(2026, 8, 25, 11, 4, 0)
        };
        var abierta = new StgSapOrden { OrdenSapId = "230000000300", EstadoOrden = "En Proceso", FechaFinReal = new DateTime(2026, 8, 1) };
        ReglasVentana Reglas(bool finReal) => new(new HashSet<string>(), true, false, new HashSet<string> { "Cerrada" },
            new HashSet<string>(), new Dictionary<string, string>(), AvancePorFinReal: finReal);

        Assert.Equal(new DateTime(2026, 7, 17), ReglasPortal.OrdenTerminada(orden, Reglas(true)));
        Assert.Equal(new DateTime(2026, 8, 25, 11, 4, 0), ReglasPortal.OrdenTerminada(orden, Reglas(false)));
        Assert.Null(ReglasPortal.OrdenTerminada(abierta, Reglas(true)));   // sin cerrar no cuenta
    }
}
