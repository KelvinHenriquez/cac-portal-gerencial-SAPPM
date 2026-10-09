using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortalGerencialBackEnd.Core.Dtos.Sap;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Infrastructure.Integraciones.Excel;

namespace PortalGerencialBackEnd.Infrastructure.Integraciones.Produccion
{
    /// <summary>Conexión a los servicios web SOAP de los módulos de función del ABAP (sección SapWs).</summary>
    public class SapWsSettings
    {
        /// <summary>https://vhazcdc3ci.sap.cac.com:44300 (sin barra final).</summary>
        public string BaseUrl { get; set; } = "";

        /// <summary>Mandante (sap-client): 320 en desarrollo.</summary>
        public string Cliente { get; set; } = "320";

        /// <summary>Usuario técnico de SAP; la clave va SOLO en el .env del servidor.</summary>
        public string Usuario { get; set; } = "";
        public string Password { get; set; } = "";

        public int TimeoutSegundos { get; set; } = 300;

        /// <summary>false solo para pruebas si el certificado es de una CA interna aún no instalada.</summary>
        public bool ValidarCertificado { get; set; } = true;

        /// <summary>
        /// Certificado raíz de la CA interna de CAC (.cer / .pem) con el que SAP firma su certificado
        /// HTTPS. Con él el portal valida la conexión sin desactivar nada (08/10/2026: "UntrustedRoot").
        /// Se confía en TODOS los certificados (.cer/.crt/.pem) de la carpeta de este archivo: así
        /// conviven el de desarrollo y el de producción (09/10/2026: SAP PRD usa otro certificado).
        /// </summary>
        public string? CertificadoCa { get; set; }

        /// <summary>Nombre del parámetro de entrada "fecha desde" (WSDL del 07/10/2026: IV_FECHA_DESDE, char8).</summary>
        public string ParametroFechaDesde { get; set; } = "IV_FECHA_DESDE";

        /// <summary>Módulos que además exigen "fecha hasta" (en el WSDL no es opcional): se manda mañana.</summary>
        public Dictionary<string, string> ParametroFechaHasta { get; set; } = new()
        {
            ["Z_PM_MF_WS_AVISO_MANTENIMIENTO"] = "IV_FECHA_HASTA"
        };

        /// <summary>
        /// Tablas de salida que hay que mandar vacías en la petición, por módulo (en los servicios
        /// generados desde un RFC, SAP solo devuelve las tablas que vienen en la petición).
        /// </summary>
        public Dictionary<string, string[]> TablasEnPeticion { get; set; } = new();

        /// <summary>Ruta de cada servicio si no sigue el patrón /sap/bc/srt/rfc/sap/{fm}/{cliente}/{zb_..}/{zb_..}.</summary>
        public Dictionary<string, string> Rutas { get; set; } = new();
    }

    /// <summary>
    /// DataSourceMode=Production: SAP por los servicios web SOAP que el ABAP generó desde los módulos
    /// de función (03/10/2026: se decidió servicios web en vez de RFC con NCo; corre en el contenedor
    /// Linux sin librerías de SAP). Cada respuesta se convierte con las mismas reglas que el
    /// importador de Excel del Sprint 0, así el staging es idéntico venga de donde venga.
    /// </summary>
    public class SapWebServiceExtractionClient : ISapExtractionClient
    {
        public const string Equipos = "Z_PM_MF_WS_MAESTRO_EQUIPO";
        public const string Ordenes = "Z_PM_MF_WS_ORDEN_MANTENIMIENTO";
        public const string Operaciones = "Z_PM_MF_WS_OPERACION_ORDEN";
        public const string Avisos = "Z_PM_MF_WS_AVISO_MANTENIMIENTO";
        public const string Historial = "Z_PM_MF_WS_HISTORIAL_ESTADO";

        private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace Rfc = "urn:sap-com:document:sap:rfc:functions";

        private readonly SapWsSettings _cfg;
        private readonly HttpClient _http;
        private readonly ILogger<SapWebServiceExtractionClient> _logger;
        private readonly Dictionary<string, List<TablaExcel>> _historialPorDesde = new();
        // Por qué se rechazó el último certificado de SAP, para decirlo en el error.
        private string? _ultimoRechazo;

        public SapWebServiceExtractionClient(IConfiguration configuration, ILogger<SapWebServiceExtractionClient> logger, HttpMessageHandler? handler = null)
        {
            _cfg = configuration.GetSection("SapWs").Get<SapWsSettings>() ?? new SapWsSettings();
            _logger = logger;
            _http = new HttpClient(handler ?? CrearHandler(_cfg, m => _ultimoRechazo = m), disposeHandler: handler == null)
            {
                Timeout = TimeSpan.FromSeconds(_cfg.TimeoutSegundos)
            };
            if (_cfg.Usuario.Length > 0)
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_cfg.Usuario}:{_cfg.Password}")));
        }

        public async Task<List<EquipoSapDto>> GetEquiposAsync(CancellationToken ct = default) =>
            Procesar(new() { ["EQUIPOS"] = await LlamarAsync(Equipos, null, ct) }).Equipos;

        public async Task<List<OrdenSapDto>> GetOrdenesModificadasDesdeAsync(DateTime desde, CancellationToken ct = default)
        {
            // El historial da la hora exacta de creación y de cierre técnico si el módulo de órdenes no la trae.
            var tablas = new Dictionary<string, List<TablaExcel>>
            {
                ["ORDENES"] = await LlamarAsync(Ordenes, desde, ct),
                ["HISTORIAL"] = await HistorialAsync(desde, ct)
            };
            return Procesar(tablas).Ordenes;
        }

        public async Task<List<OperacionSapDto>> GetOperacionesModificadasDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            Procesar(new() { ["OPERACIONES"] = await LlamarAsync(Operaciones, desde, ct) }).Operaciones;

        public async Task<List<AvisoSapDto>> GetAvisosModificadosDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            Procesar(new() { ["AVISOS"] = await LlamarAsync(Avisos, desde, ct) }).Avisos;

        public async Task<List<HistorialEstadoDto>> GetHistorialEstadosDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            Procesar(new() { ["HISTORIAL"] = await HistorialAsync(desde, ct) }).Historial;

        // ------------------------------------------------------------ SOAP

        private async Task<List<TablaExcel>> HistorialAsync(DateTime desde, CancellationToken ct)
        {
            var clave = desde.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            if (!_historialPorDesde.TryGetValue(clave, out var tablas))
                _historialPorDesde[clave] = tablas = await LlamarAsync(Historial, desde, ct);
            return tablas;
        }

        private SapExcelExtractionClient.Datos Procesar(Dictionary<string, List<TablaExcel>> tablas)
        {
            var datos = SapExcelExtractionClient.ProcesarPorTipo(tablas, new SapExcelExtractionClient.Datos(), DateTime.Now, soloLoQueVino: true);
            // Sin filas (nada cambió en SAP desde la fecha pedida) no hay nada que advertir.
            if (tablas.Values.Sum(l => l.Sum(t => t.Filas.Count)) == 0)
                return datos;
            foreach (var m in datos.Mensajes)
                _logger.LogWarning("SAP (servicios web): {Mensaje}", m);
            return datos;
        }

        internal string Url(string modulo)
        {
            if (string.IsNullOrWhiteSpace(_cfg.BaseUrl))
                throw new InvalidOperationException("Falta SapWs:BaseUrl (URL del servidor SAP, p. ej. https://vhazcdc3ci.sap.cac.com:44300).");
            if (string.IsNullOrWhiteSpace(_cfg.Cliente))
                throw new InvalidOperationException("Falta SapWs:Cliente (mandante de SAP, p. ej. 320).");
            var minus = modulo.ToLowerInvariant();
            var binding = "zb" + minus[1..];   // z_pm_mf_ws_x -> zb_pm_mf_ws_x
            var ruta = _cfg.Rutas.GetValueOrDefault(modulo) ?? $"/sap/bc/srt/rfc/sap/{minus}/{_cfg.Cliente}/{binding}/{binding}";
            return $"{_cfg.BaseUrl.TrimEnd('/')}{ruta}?sap-client={_cfg.Cliente}";
        }

        internal string Peticion(string modulo, DateTime? desde)
        {
            var parametros = new List<XElement>();
            if (desde != null)
            {
                parametros.Add(new XElement(_cfg.ParametroFechaDesde, desde.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture)));
                if (_cfg.ParametroFechaHasta.TryGetValue(modulo, out var hasta))
                    parametros.Add(new XElement(hasta, DateTime.Today.AddDays(1).ToString("yyyyMMdd", CultureInfo.InvariantCulture)));
            }
            foreach (var tabla in _cfg.TablasEnPeticion.GetValueOrDefault(modulo) ?? Array.Empty<string>())
                parametros.Add(new XElement(tabla));
            var sobre = new XElement(Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", Soap),
                new XAttribute(XNamespace.Xmlns + "urn", Rfc),
                new XElement(Soap + "Header"),
                new XElement(Soap + "Body", new XElement(Rfc + modulo, parametros)));
            return sobre.ToString(SaveOptions.DisableFormatting);
        }

        private async Task<List<TablaExcel>> LlamarAsync(string modulo, DateTime? desde, CancellationToken ct)
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Post, Url(modulo))
            {
                Content = new StringContent(Peticion(modulo, desde), Encoding.UTF8, "text/xml")
            };
            // SOAPAction tal como lo publica el WSDL de cada servicio.
            peticion.Headers.Add("SOAPAction", $"urn:sap-com:document:sap:rfc:functions:{modulo}:{modulo}Request");

            HttpResponseMessage respuesta;
            try
            {
                respuesta = await _http.SendAsync(peticion, ct);
            }
            catch (HttpRequestException ex)
            {
                var detalle = ex.InnerException?.Message ?? "";
                var carpeta = string.IsNullOrWhiteSpace(_cfg.CertificadoCa) ? "SapWs:CertificadoCa" : Path.GetDirectoryName(_cfg.CertificadoCa);
                var ayuda = _ultimoRechazo != null
                    ? $" {_ultimoRechazo} Si ese certificado es el de SAP, guárdelo (.cer o .pem) en {carpeta}."
                    : detalle.Contains("UntrustedRoot") || detalle.Contains("certificate chain")
                    ? $" El certificado de SAP es de una CA que el portal no conoce (la CA interna de CAC): exporte el certificado raíz"
                      + $" desde el navegador y guárdelo en {carpeta}."
                    : "";
                throw new InvalidOperationException($"No se pudo conectar con SAP ({modulo}): {ex.Message}{ayuda}", ex);
            }
            using (respuesta)
            {
                var cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
                if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
                    throw new InvalidOperationException($"SAP rechazó el usuario o la clave ({modulo}). Revise SapWs:Usuario / SapWs:Password.");
                var tablas = Leer(modulo, cuerpo, respuesta.IsSuccessStatusCode ? null : (int)respuesta.StatusCode);
                _logger.LogInformation("SAP {Modulo}: {Filas} filas.", modulo, tablas.Sum(t => t.Filas.Count));
                return tablas;
            }
        }

        /// <summary>
        /// Respuesta SOAP → tablas: cada parámetro de salida con elementos &lt;item&gt; es una tabla cuyas
        /// columnas son los nombres de campo (EQUNR, AUFNR...). Un SOAP Fault se informa con su texto.
        /// </summary>
        internal static List<TablaExcel> Leer(string modulo, string xml, int? codigoHttp = null)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"SAP devolvió una respuesta que no es XML ({modulo}{(codigoHttp != null ? $", HTTP {codigoHttp}" : "")}): {xml[..Math.Min(200, xml.Length)]}", ex);
            }
            var falla = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
            if (falla != null)
            {
                var texto = falla.Descendants().FirstOrDefault(e => e.Name.LocalName is "faultstring" or "Text")?.Value ?? falla.Value;
                throw new InvalidOperationException($"SAP devolvió un error en {modulo}: {texto}");
            }
            if (codigoHttp != null)
                throw new InvalidOperationException($"SAP respondió HTTP {codigoHttp} en {modulo}.");

            var resultado = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == modulo + "Response")
                            ?? throw new InvalidOperationException($"La respuesta de SAP no trae {modulo}Response.");
            return resultado.Elements()
                .Where(t => t.Elements().Any(i => i.Name.LocalName == "item"))
                .Select(t =>
                {
                    var filas = t.Elements().Where(i => i.Name.LocalName == "item")
                        .Select(i => new FilaExcel(i.Elements()
                            .GroupBy(c => LectorTablaExcel.Normalizar(c.Name.LocalName))
                            .ToDictionary(g => g.Key, g => (object?)g.First().Value.Trim())))
                        .ToList();
                    return new TablaExcel
                    {
                        Origen = $"{modulo}.{t.Name.LocalName}",
                        Encabezados = filas.SelectMany(f => f.Columnas).ToHashSet(),
                        Filas = filas
                    };
                })
                .ToList();
        }

        private static HttpMessageHandler CrearHandler(SapWsSettings cfg, Action<string> alRechazar)
        {
            var handler = new HttpClientHandler();
            if (!cfg.ValidarCertificado)
            {
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                return handler;
            }
            // Si el archivo todavía no está, se valida con las CA del sistema (y el error lo explica).
            var raices = CargarRaices(cfg.CertificadoCa);
            if (raices.Count > 0)
                handler.ServerCertificateCustomValidationCallback = (_, certificado, cadena, errores) =>
                {
                    if (certificado == null) return false;
                    if (ValidarConCa(certificado, raices, errores, cadena, out var motivo)) return true;
                    alRechazar(motivo);
                    return false;
                };
            return handler;
        }

        /// <summary>
        /// Certificados de confianza: todos los .cer/.crt/.pem de la carpeta de SapWs:CertificadoCa (o
        /// de esa carpeta si apunta a una). Cada archivo puede traer uno o varios, en PEM o binario (DER).
        /// </summary>
        internal static X509Certificate2Collection CargarRaices(string? ruta)
        {
            var raices = new X509Certificate2Collection();
            if (string.IsNullOrWhiteSpace(ruta)) return raices;
            var carpeta = Directory.Exists(ruta) ? ruta : File.Exists(ruta) ? Path.GetDirectoryName(ruta) : null;
            if (carpeta == null) return raices;
            foreach (var archivo in Directory.GetFiles(carpeta).Where(a =>
                         new[] { ".cer", ".crt", ".pem" }.Contains(Path.GetExtension(a).ToLowerInvariant())).OrderBy(a => a))
            {
                if (File.ReadAllText(archivo).Contains("-----BEGIN CERTIFICATE-----"))
                    raices.ImportFromPemFile(archivo);
                else
                    raices.Import(File.ReadAllBytes(archivo));
            }
            return raices;
        }

        /// <summary>
        /// El certificado del servidor es válido si encadena a la CA de CAC indicada (además del resto de
        /// comprobaciones: nombre del servidor, vigencia). Solo se tolera el error de "raíz no confiable".
        /// </summary>
        internal static bool ValidarConCa(X509Certificate2 certificado, X509Certificate2 raiz, SslPolicyErrors errores)
            => ValidarConCa(certificado, new X509Certificate2Collection(raiz), errores, null, out _);

        internal static bool ValidarConCa(X509Certificate2 certificado, X509Certificate2Collection raices, SslPolicyErrors errores,
            X509Chain? cadenaDelServidor, out string motivo)
        {
            motivo = "";
            if (errores == SslPolicyErrors.None) return true;
            var quien = $"SAP presentó el certificado \"{certificado.Subject}\", emitido por \"{certificado.Issuer}\"" +
                        $" (vence {certificado.NotAfter:dd/MM/yyyy}, huella {certificado.Thumbprint}).";
            if ((errores & ~SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                motivo = $"{quien} Rechazado: {errores} (el nombre del servidor no coincide o no envió certificado).";
                return false;
            }
            // Certificado fijado: SAP presenta exactamente uno de los guardados. Hace falta para el de
            // producción (09/10/2026): es autofirmado pero X.509 v3 marcado como de servidor, y .NET no
            // lo acepta como raíz de una cadena (el de desarrollo es v1 y sí). Sigue exigiendo el nombre
            // del servidor (arriba) y la vigencia.
            var ahora = DateTime.Now;
            if (raices.Any(r => r.RawData.AsSpan().SequenceEqual(certificado.RawData)))
            {
                if (ahora >= certificado.NotBefore && ahora <= certificado.NotAfter) return true;
                motivo = $"{quien} Es uno de los certificados guardados, pero no está vigente.";
                return false;
            }
            using var cadena = new X509Chain();
            cadena.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            cadena.ChainPolicy.CustomTrustStore.AddRange(raices);
            cadena.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            // Intermedios que mandó el propio servidor.
            if (cadenaDelServidor != null)
                foreach (var e in cadenaDelServidor.ChainElements)
                    if (e.Certificate.Thumbprint != certificado.Thumbprint)
                        cadena.ChainPolicy.ExtraStore.Add(e.Certificate);
            if (cadena.Build(certificado)) return true;
            motivo = $"{quien} No encadena a ninguno de los {raices.Count} certificado(s) de confianza del portal" +
                     $" ({string.Join(", ", cadena.ChainStatus.Select(c => c.Status).Distinct())}).";
            return false;
        }
    }
}
