using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PortalGerencialBackEnd.Infrastructure.Integraciones.Produccion;

namespace PortalGerencialBackEnd.Tests;

/// <summary>Cliente de servicios web de SAP con respuestas armadas como las de SOAMANAGER.</summary>
public class SapWebServiceTests
{
    private sealed class SapFalso : HttpMessageHandler
    {
        public readonly List<(string Url, string Cuerpo, string? Auth)> Peticiones = new();
        public Func<string, (HttpStatusCode, string)> Responder = _ => (HttpStatusCode.OK, "");

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var cuerpo = await request.Content!.ReadAsStringAsync(ct);
            Peticiones.Add((request.RequestUri!.ToString(), cuerpo, request.Headers.Authorization?.ToString()));
            var modulo = request.RequestUri!.AbsolutePath.Split('/')[6].ToUpperInvariant();
            var (codigo, xml) = Responder(modulo);
            return new HttpResponseMessage(codigo) { Content = new StringContent(xml, Encoding.UTF8, "text/xml") };
        }
    }

    private static string Respuesta(string modulo, string tabla, params string[] items) =>
        $"""
        <soap-env:Envelope xmlns:soap-env="http://schemas.xmlsoap.org/soap/envelope/"><soap-env:Header/><soap-env:Body>
        <n0:{modulo}Response xmlns:n0="urn:sap-com:document:sap:rfc:functions">
        <{tabla}>{string.Concat(items.Select(i => $"<item>{i}</item>"))}</{tabla}>
        </n0:{modulo}Response></soap-env:Body></soap-env:Envelope>
        """;

    private static (SapWebServiceExtractionClient Cliente, SapFalso Sap) Crear()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SapWs:BaseUrl"] = "https://sap.prueba.local:44300",
            ["SapWs:Cliente"] = "320",
            ["SapWs:Usuario"] = "PORTAL_PM",
            ["SapWs:Password"] = "clave-de-prueba"
        }).Build();
        var sap = new SapFalso();
        return (new SapWebServiceExtractionClient(config, NullLogger<SapWebServiceExtractionClient>.Instance, sap), sap);
    }

    [Fact]
    public async Task Llama_al_servicio_del_modulo_con_la_fecha_en_yyyymmdd_y_autenticacion_basica()
    {
        var (cliente, sap) = Crear();
        sap.Responder = m => (HttpStatusCode.OK, Respuesta(m, "ET_AVISOS",
            "<QMNUM>000010000002</QMNUM><QMART>Z2</QMART><EQUNR>012-052002-0023</EQUNR><AUFNR>000210000000255</AUFNR>" +
            "<QMDAT>2026-07-23</QMDAT><MZEIT>09:21:45</MZEIT><MSAUS>X</MSAUS><AUSVN>2026-07-23</AUSVN><AUZTV>09:00:00</AUZTV>" +
            "<AUSBS>0000-00-00</AUSBS><AUZTB>00:00:00</AUZTB><AEDAT>2026-07-23</AEDAT>"));

        var avisos = await cliente.GetAvisosModificadosDesdeAsync(new DateTime(2026, 7, 1));

        var (url, cuerpo, auth) = sap.Peticiones.Single();
        Assert.Equal("https://sap.prueba.local:44300/sap/bc/srt/rfc/sap/z_pm_mf_ws_aviso_mantenimiento/320/zb_pm_mf_ws_aviso_mantenimiento/zb_pm_mf_ws_aviso_mantenimiento?sap-client=320", url);
        Assert.Contains("<IV_FECHA_DESDE>20260701</IV_FECHA_DESDE>", cuerpo);
        Assert.Contains("Z_PM_MF_WS_AVISO_MANTENIMIENTO", cuerpo);
        Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("PORTAL_PM:clave-de-prueba")), auth);

        var a = avisos.Single();
        Assert.Equal("10000002", a.AvisoSapId);
        Assert.Equal("210000000255", a.OrdenSapId);
        Assert.True(a.Parada);
        Assert.Equal(new DateTime(2026, 7, 23, 9, 21, 45), a.FechaAviso);
        Assert.Equal(new DateTime(2026, 7, 23, 9, 0, 0), a.InicioAveria);
        Assert.Null(a.FinAveria);   // 0000-00-00 = vacío
    }

    [Fact]
    public async Task Ordenes_toman_la_hora_de_cierre_tecnico_del_historial()
    {
        var (cliente, sap) = Crear();
        sap.Responder = m => (HttpStatusCode.OK, m switch
        {
            SapWebServiceExtractionClient.Ordenes => Respuesta(m, "ET_ORDENES",
                "<AUFNR>000210000000255</AUFNR><AUART>ZC01</AUART><EQUNR>012-052002-0023</EQUNR><KTEXT>Fuga de aceite</KTEXT>" +
                "<STATUS_SISTEMA>CTEC NOTI</STATUS_SISTEMA><ERDAT>2026-07-23</ERDAT><ERFZEIT>11:01:00</ERFZEIT><QMNUM>000010000002</QMNUM>"),
            SapWebServiceExtractionClient.Historial => Respuesta(m, "ET_HISTORIAL",
                "<AUFNR>000210000000255</AUFNR><STAT>I0045</STAT><CHGNR>001</CHGNR><UDATE>2026-07-23</UDATE><UTIME>16:23:00</UTIME><CHIND>I</CHIND><INACT></INACT>",
                "<AUFNR>000210000000255</AUFNR><STAT>E0003</STAT><CHGNR>002</CHGNR><UDATE>2026-07-23</UDATE><UTIME>15:00:00</UTIME><CHIND>I</CHIND><INACT></INACT>"),
            _ => throw new InvalidOperationException(m)
        });

        var orden = (await cliente.GetOrdenesModificadasDesdeAsync(new DateTime(2026, 7, 1))).Single();
        var historial = await cliente.GetHistorialEstadosDesdeAsync(new DateTime(2026, 7, 1));

        Assert.Equal("Cerrada", orden.EstadoOrden);
        Assert.Equal(new DateTime(2026, 7, 23, 11, 1, 0), orden.FechaCreacion);
        Assert.Equal(new DateTime(2026, 7, 23, 16, 23, 0), orden.FechaCierre);
        Assert.Equal("10000002", orden.AvisoSapId);
        Assert.Equal(2, historial.Count);
        Assert.Equal(2, sap.Peticiones.Count);   // el historial se pide una sola vez por corrida
    }

    [Fact]
    public async Task Maestro_sin_fecha_y_con_la_ficha_desde_la_descripcion()
    {
        var (cliente, sap) = Crear();
        sap.Responder = m => (HttpStatusCode.OK, Respuesta(m, "ET_EQUIPOS",
            "<EQUNR>012-052002-0023</EQUNR><EQKTX>Motobomba John Deere F01198</EQKTX><KEY_NUM></KEY_NUM><EQART>PM_MOTOB</EQART>" +
            "<EQART_DESC>Motobomba</EQART_DESC><INGRP>CDS</INGRP><INGRP_DESC>Centro de Servicio</INGRP_DESC><ESTADO_EQUIPO>Activo</ESTADO_EQUIPO>"));

        var e = (await cliente.GetEquiposAsync()).Single();

        Assert.DoesNotContain("IV_FECHA_DESDE", sap.Peticiones.Single().Cuerpo);
        Assert.Equal("F01198", e.CodigoLegado);
        Assert.Equal("CDS", e.GrupoPlanificacion);
        Assert.Equal("Motobomba", e.TipoEquipoDesc);
        Assert.False(e.Inactivo);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "", "rechazó el usuario o la clave")]
    [InlineData(HttpStatusCode.InternalServerError,
        "<soap-env:Envelope xmlns:soap-env=\"http://schemas.xmlsoap.org/soap/envelope/\"><soap-env:Body><soap-env:Fault><faultcode>soap-env:Client</faultcode><faultstring>Fecha no válida</faultstring></soap-env:Fault></soap-env:Body></soap-env:Envelope>",
        "Fecha no válida")]
    public async Task Los_errores_de_SAP_se_informan_con_su_motivo(HttpStatusCode codigo, string xml, string esperado)
    {
        var (cliente, sap) = Crear();
        sap.Responder = _ => (codigo, xml);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => cliente.GetAvisosModificadosDesdeAsync(new DateTime(2026, 7, 1)));
        Assert.Contains(esperado, ex.Message);
    }
}
