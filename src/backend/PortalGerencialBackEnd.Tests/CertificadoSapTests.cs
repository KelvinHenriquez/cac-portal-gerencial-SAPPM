using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PortalGerencialBackEnd.Infrastructure.Integraciones.Produccion;

namespace PortalGerencialBackEnd.Tests;

/// <summary>Certificado HTTPS de SAP firmado por la CA interna de CAC.</summary>
public class CertificadoSapTests
{
    private static X509Certificate2 Ca(string nombre)
    {
        using var clave = RSA.Create(2048);
        var req = new CertificateRequest($"CN={nombre}", clave, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
        return req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(5));
    }

    private static X509Certificate2 Servidor(X509Certificate2 ca)
    {
        using var clave = RSA.Create(2048);
        var req = new CertificateRequest("CN=vhazcdc3ci.sap.cac.com", clave, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        return req.Create(ca, DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(1), Guid.NewGuid().ToByteArray());
    }

    [Fact]
    public void Acepta_el_certificado_de_SAP_si_encadena_a_la_CA_de_CAC_y_rechaza_otra()
    {
        var caCac = Ca("CAC Root CA");
        var otraCa = Ca("Otra CA");
        var servidor = Servidor(caCac);

        Assert.True(SapWebServiceExtractionClient.ValidarConCa(servidor, caCac, SslPolicyErrors.RemoteCertificateChainErrors));
        Assert.False(SapWebServiceExtractionClient.ValidarConCa(servidor, otraCa, SslPolicyErrors.RemoteCertificateChainErrors));
        // Un nombre de servidor que no coincide nunca se tolera.
        Assert.False(SapWebServiceExtractionClient.ValidarConCa(servidor, caCac,
            SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch));
    }

    [Fact]
    public void Confia_en_todos_los_certificados_de_la_carpeta_y_explica_el_rechazo()
    {
        var caDev = Ca("SAP DEV");
        var caPrd = Ca("SAP PRD");
        var carpeta = Directory.CreateTempSubdirectory("certs-sap").FullName;
        try
        {
            // DEV en PEM (como ca_cac.cer) y PRD en binario (DER), como lo exporta el navegador.
            File.WriteAllText(Path.Combine(carpeta, "ca_cac.cer"), caDev.ExportCertificatePem());
            File.WriteAllBytes(Path.Combine(carpeta, "sap_prd.cer"), caPrd.Export(X509ContentType.Cert));
            File.WriteAllText(Path.Combine(carpeta, "LEEME.txt"), "no es un certificado");

            var raices = SapWebServiceExtractionClient.CargarRaices(Path.Combine(carpeta, "ca_cac.cer"));
            Assert.Equal(2, raices.Count);
            Assert.True(SapWebServiceExtractionClient.ValidarConCa(Servidor(caDev), raices, SslPolicyErrors.RemoteCertificateChainErrors, null, out _));
            Assert.True(SapWebServiceExtractionClient.ValidarConCa(Servidor(caPrd), raices, SslPolicyErrors.RemoteCertificateChainErrors, null, out _));

            var ajeno = Servidor(Ca("Otra CA"));
            Assert.False(SapWebServiceExtractionClient.ValidarConCa(ajeno, raices, SslPolicyErrors.RemoteCertificateChainErrors, null, out var motivo));
            Assert.Contains("Otra CA", motivo);
            Assert.Contains(ajeno.Thumbprint, motivo);
        }
        finally
        {
            Directory.Delete(carpeta, true);
        }
    }
}
