using PortalGerencialBackEnd.Core.Dtos.Sap;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;

namespace PortalGerencialBackEnd.Infrastructure.Integraciones.Produccion
{
    /// <summary>
    /// DataSourceMode=Production: llama por RFC a los cinco módulos Z del Anexo A v2 con SAP .NET
    /// Connector (NCo 3.1). Es la única clase del portal que sabe que SAP existe (Anexo A.6).
    ///
    /// Se implementa en la Etapa 8, cuando estén:
    /// - los módulos Z_PM_TALLERES_{EQUIPOS,ORDENES,OPERACIONES,AVISOS,HISTORIAL_ESTADO} marcados como
    ///   Remote-Enabled (el ABAP los está desarrollando);
    /// - el usuario de comunicación con S_RFC y la ruta de red al gateway (33xx);
    /// - la librería NCo y la decisión de dónde corre (contenedor Linux o servicio Windows,
    ///   ver docs/ETAPAS.md).
    /// Cada método recibe IV_FECHA_DESDE = fecha de la última extracción OK de esa fuente.
    /// </summary>
    public class SapRfcExtractionClient : ISapExtractionClient
    {
        private const string Mensaje =
            "La extracción real de SAP (RFC/NCo) se implementa en la Etapa 8. Use DataSourceMode=Local mientras tanto.";

        public Task<List<EquipoSapDto>> GetEquiposAsync(CancellationToken ct = default) =>
            throw new NotImplementedException(Mensaje);

        public Task<List<OrdenSapDto>> GetOrdenesModificadasDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            throw new NotImplementedException(Mensaje);

        public Task<List<OperacionSapDto>> GetOperacionesModificadasDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            throw new NotImplementedException(Mensaje);

        public Task<List<AvisoSapDto>> GetAvisosModificadosDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            throw new NotImplementedException(Mensaje);

        public Task<List<HistorialEstadoDto>> GetHistorialEstadosDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            throw new NotImplementedException(Mensaje);
    }
}
