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
        /// <param name="desde">Período del MTTR/MTBF de cada equipo (por defecto, los últimos 90 días).</param>
        Task<EquiposAsignacionDto> GetEquiposAsync(int reporteId, int asignacionId, DateTime? desde = null, DateTime? hasta = null);
        /// <summary>MTTR y MTBF por asignación y de "Todas" entre esas fechas (las semanas visibles).</summary>
        Task<ConfiabilidadReporteDto> GetConfiabilidadAsync(int reporteId, DateTime desde, DateTime hasta);
    }
}
