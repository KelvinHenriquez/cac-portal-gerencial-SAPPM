using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Infrastructure.Services;

namespace PortalGerencialBackEnd.Tests;

public class CatalogoParametrosTests
{
    [Theory]
    [InlineData(ClavesParametro.BasePorcentajeDisponibilidad, "equipos", "EQUIPOS", null)]
    [InlineData(ClavesParametro.BasePorcentajeDisponibilidad, "TOTAL", null, "Valores permitidos")]
    [InlineData(ClavesParametro.HoraCorridaDiaria, "05:30", "05:30", null)]
    [InlineData(ClavesParametro.HoraCorridaDiaria, "5", null, "HH:mm")]
    [InlineData(ClavesParametro.HorasAlertaFrescura, "0", null, "mayor que cero")]
    [InlineData(ClavesParametro.ClasesOrdenDisponibilidad, " ZC01, ZC02 ", "ZC01, ZC02", null)]
    [InlineData(ClavesParametro.TipoPorClaseOrden, "ZC01=CORRECTIVA,ZC02=PREVENTIVO", "ZC01=CORRECTIVA,ZC02=PREVENTIVO", null)]
    [InlineData(ClavesParametro.TipoPorClaseOrden, "ZC01=URGENTE", null, "CLASE=TIPO")]
    [InlineData(ClavesParametro.FechaCargaInicial, "01/06/2026", null, "aaaa-mm-dd")]
    [InlineData(ClavesParametro.EstadosCerrados, "   ", null, "vacío")]
    public void Valida_y_normaliza_cada_parametro(string clave, string valor, string? esperado, string? error)
    {
        var (v, e) = CatalogoParametros.Validar(clave, valor);
        Assert.Equal(esperado, v);
        if (error == null) Assert.Null(e);
        else Assert.Contains(error, e);
    }
}
