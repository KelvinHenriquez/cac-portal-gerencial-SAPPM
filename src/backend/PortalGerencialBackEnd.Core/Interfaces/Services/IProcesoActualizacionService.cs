using PortalGerencialBackEnd.Core.Dtos.Sistema;

namespace PortalGerencialBackEnd.Core.Interfaces.Services
{
    /// <summary>
    /// Una corrida completa del job (sección 6): extrae de SAP al staging y recalcula
    /// disponibilidad, avance y equipos sin clasificar. Cada paso queda en LOG_EXTRACCION_PGPM.
    /// Si una extracción falla, la corrida se detiene y los resultados calculados anteriores
    /// quedan intactos (sección 10).
    /// </summary>
    public interface IProcesoActualizacionService
    {
        /// <param name="origen">PROGRAMADA (Background Service) o MANUAL (botón en el portal).</param>
        Task<CorridaDto> EjecutarAsync(string origen, CancellationToken ct = default);
    }

    public interface ISistemaService
    {
        Task<EstadoActualizacionDto> GetEstadoActualizacionAsync();
        Task<List<CorridaDto>> GetCorridasAsync(int cantidad);
        Task<CorridaDto?> GetCorridaAsync(string corridaId);
        Task<List<EquipoSinClasificarDto>> GetEquiposSinClasificarAsync();
    }
}
