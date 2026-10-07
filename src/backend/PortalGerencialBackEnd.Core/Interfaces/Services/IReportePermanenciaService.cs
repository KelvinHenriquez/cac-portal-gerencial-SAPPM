using PortalGerencialBackEnd.Core.Dtos.Permanencia;

namespace PortalGerencialBackEnd.Core.Interfaces.Services
{
    /// <summary>Reporte "Tiempo de Permanencia": lee CALC_PERMANENCIA_PGPM y agrega al vuelo.</summary>
    public interface IReportePermanenciaService
    {
        Task<List<AreaPermanenciaDto>> GetAreasAsync();
        Task<ResumenPermanenciaDto> GetResumenAsync(string area, DateTime? desde, DateTime? hasta);
        Task<FamiliaPermanenciaDto> GetFamiliaAsync(string area, string tipoEquipo, DateTime? desde, DateTime? hasta);
        Task<EquipoPermanenciaDto> GetEquipoAsync(string equipoSapId, DateTime? desde, DateTime? hasta);
    }
}
