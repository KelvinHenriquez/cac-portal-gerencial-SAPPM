using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Dtos.Auth;

namespace PortalGerencialBackEnd.Tests;

/// <summary>Matriz de roles del 10/10/2026: qué ve y qué puede cada uno.</summary>
public class PermisosTests
{
    [Theory]
    //          roles (separados por coma)                     áreas      parámetros  técnicos  actualizar
    [InlineData("ROL_PORTAL_CDS",                              "CDS",     false,      false,    false)]
    [InlineData("ROL_PORTAL_IND",                              "IND",     false,      false,    false)]
    [InlineData("ROL_PORTAL_GERENCIAL",                        "CDS,IND", false,      false,    false)]
    [InlineData("ROL_PORTAL_CDS,ROL_PORTAL_PARAMETROS",        "CDS",     true,       false,    false)]
    [InlineData("ROL_PORTAL_PARAMETROS",                       "",        true,       false,    false)]
    [InlineData("ROL_PORTAL_ADMIN",                            "CDS,IND", true,       true,     true)]
    [InlineData("rol_portal_cds,ROL_PORTAL_IND",               "CDS,IND", false,      false,    false)]
    public void Cada_rol_ve_y_puede_lo_suyo(string roles, string areas, bool parametros, bool tecnicos, bool actualizar)
    {
        var p = PermisosDto.De(roles.Split(','));
        Assert.Equal(areas.Split(',', StringSplitOptions.RemoveEmptyEntries), p.Areas);
        Assert.Equal(parametros, p.EditarParametros);
        Assert.Equal(tecnicos, p.EditarParametrosTecnicos);
        Assert.Equal(actualizar, p.Actualizar);
    }

    [Fact]
    public void Ve_area_sin_importar_mayusculas()
    {
        Assert.True(PermisosDto.De(new[] { Roles.Industrial }).VeArea("ind"));
        Assert.False(PermisosDto.De(new[] { Roles.Industrial }).VeArea("CDS"));
    }
}
