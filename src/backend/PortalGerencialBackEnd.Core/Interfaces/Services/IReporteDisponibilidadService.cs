using PortalGerencialBackEnd.Core.Dtos.Disponibilidad;

namespace PortalGerencialBackEnd.Core.Interfaces.Services
{
    /// <summary>
    /// Sección 5: lee resultados ya calculados (calc_*_pgpm) y el staging; nunca llama a SAP.
    /// </summary>
    public interface IReporteDisponibilidadService
    {
        Task<ReporteDisponibilidadDto> GetReporteAsync(int reporteId);
        Task<DetalleSemanaDto> GetDetalleSemanaAsync(int reporteId, int asignacionId, int noSemana);
        Task<List<HoraDisponibilidadDto>> GetHorasAsync(int reporteId, int asignacionId, DateTime fecha);
        Task<EquiposAsignacionDto> GetEquiposAsync(int reporteId, int asignacionId);
    }
}
