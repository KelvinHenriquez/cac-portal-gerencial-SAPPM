using PortalGerencialBackEnd.Core.Dtos.Avance;

namespace PortalGerencialBackEnd.Core.Interfaces.Services
{
    /// <summary>Sección 5: lee CALC_AVANCE_SEMANAL_PGPM (ya calculado) y el staging; nunca llama a SAP.</summary>
    public interface IReporteAvanceService
    {
        /// <param name="medida">ORDENES o ACTIVIDADES.</param>
        /// <param name="refPlan">EJECUTADO, PROGRAMADO o NO_PROGRAMADO (solo con ACTIVIDADES).</param>
        Task<ReporteAvanceDto> GetReporteAsync(int reporteId, string medida, string refPlan);

        Task<DetalleSemanaAvanceDto> GetDetalleSemanaAsync(int reporteId, string familiaCodigo, int noSemana);

        Task<AnalisisAvanceDto> GetAnalisisAsync(int reporteId, string familiaCodigo);
    }
}
