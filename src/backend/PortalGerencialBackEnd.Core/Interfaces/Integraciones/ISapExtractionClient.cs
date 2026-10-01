using PortalGerencialBackEnd.Core.Dtos.Sap;

namespace PortalGerencialBackEnd.Core.Interfaces.Integraciones
{
    /// <summary>
    /// SAP PM, solo lectura (sección 5 y Anexo A.6). Lo usa únicamente el proceso de
    /// actualización: ninguna pantalla llama a SAP. DataSourceMode decide la implementación:
    /// Local = simulador sin SAP; Production = RFC/BAPI con SAP .NET Connector.
    /// </summary>
    public interface ISapExtractionClient
    {
        /// <summary>Maestro completo de equipos (el módulo no recibe fecha de corte).</summary>
        Task<List<EquipoSapDto>> GetEquiposAsync(CancellationToken ct = default);

        Task<List<OrdenSapDto>> GetOrdenesModificadasDesdeAsync(DateTime desde, CancellationToken ct = default);

        Task<List<HistorialEstadoDto>> GetHistorialEstadosDesdeAsync(DateTime desde, CancellationToken ct = default);

        Task<List<OperacionHorasDto>> GetHorasModificadasDesdeAsync(DateTime desde, CancellationToken ct = default);
    }
}
