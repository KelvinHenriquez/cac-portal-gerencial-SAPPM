using PortalGerencialBackEnd.Core.Dtos.Sap;

namespace PortalGerencialBackEnd.Core.Interfaces.Integraciones
{
    /// <summary>
    /// SAP PM, solo lectura (sección 5 y Anexo A v2). Lo usa únicamente el proceso de
    /// actualización: ninguna pantalla llama a SAP. DataSourceMode decide la implementación:
    /// Local = simulador sin SAP; Production = RFC/BAPI con SAP .NET Connector.
    /// </summary>
    public interface ISapExtractionClient
    {
        /// <summary>Z_PM_TALLERES_EQUIPOS: maestro completo (el módulo no recibe fecha de corte).</summary>
        Task<List<EquipoSapDto>> GetEquiposAsync(CancellationToken ct = default);

        /// <summary>Z_PM_TALLERES_ORDENES.</summary>
        Task<List<OrdenSapDto>> GetOrdenesModificadasDesdeAsync(DateTime desde, CancellationToken ct = default);

        /// <summary>Z_PM_TALLERES_OPERACIONES.</summary>
        Task<List<OperacionSapDto>> GetOperacionesModificadasDesdeAsync(DateTime desde, CancellationToken ct = default);

        /// <summary>Z_PM_TALLERES_AVISOS.</summary>
        Task<List<AvisoSapDto>> GetAvisosModificadosDesdeAsync(DateTime desde, CancellationToken ct = default);

        /// <summary>Z_PM_TALLERES_HISTORIAL_ESTADO.</summary>
        Task<List<HistorialEstadoDto>> GetHistorialEstadosDesdeAsync(DateTime desde, CancellationToken ct = default);
    }
}
