using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
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

        /// <summary>Nombre del parámetro de entrada "fecha desde" (según el WSDL).</summary>
        public string ParametroFechaDesde { get; set; } = "IV_FECHA_DESDE";

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

        public SapWebServiceExtractionClient(IConfiguration configuration, ILogger<SapWebServiceExtractionClient> logger, HttpMessageHandler? handler = null)
        {
            _cfg = configuration.GetSection("SapWs").Get<SapWsSettings>() ?? new SapWsSettings();
            _logger = logger;
            _http = new HttpClient(handler ?? CrearHandler(_cfg), disposeHandler: handler == null)
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
                parametros.Add(new XElement(_cfg.ParametroFechaDesde, desde.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture)));
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
            peticion.Headers.Add("SOAPAction", $"urn:sap-com:document:sap:rfc:functions:{modulo}");

            HttpResponseMessage respuesta;
            try
            {
                respuesta = await _http.SendAsync(peticion, ct);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"No se pudo conectar con SAP ({modulo}): {ex.Message}", ex);
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

        private static HttpMessageHandler CrearHandler(SapWsSettings cfg)
        {
            var handler = new HttpClientHandler();
            if (!cfg.ValidarCertificado)
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        }
    }
}
