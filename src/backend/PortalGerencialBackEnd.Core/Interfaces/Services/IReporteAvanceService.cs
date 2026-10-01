using PortalGerencialBackEnd.Core.Dtos.Avance;

namespace PortalGerencialBackEnd.Core.Interfaces.Services
{
    /// <summary>Sección 5: lee CALC_AVANCE_SEMANAL_PGPM, ya calculado por el job.</summary>
    public interface IReporteAvanceService
    {
        Task<ReporteAvanceDto> GetAvanceAsync(string anioPrograma);
    }
}
