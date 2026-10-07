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
}
