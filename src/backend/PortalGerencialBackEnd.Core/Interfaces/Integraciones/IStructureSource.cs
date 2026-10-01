using PortalGerencialBackEnd.Core.Dtos.Estructura;

namespace PortalGerencialBackEnd.Core.Interfaces.Integraciones
{
    /// <summary>
    /// Estructura de negocio que vive en Oracle (sección 5), leída en vivo por las vistas _pgpm.
    /// Una sola implementación para local y producción: lo que cambia entre ambientes es a qué
    /// tablas apuntan las vistas, no el código.
    /// </summary>
    public interface IStructureSource
    {
        Task<List<TipoReporteDto>> GetTiposReporteAsync();
        Task<UmbralesDto?> GetUmbralesAsync(string tipoReporte);
        Task<List<ProgramaDto>> GetProgramasAsync();
        Task<List<FamiliaDto>> GetFamiliasAsync(string anioPrograma);
        Task<List<CurvaSemanaDto>> GetCurvaPlaneadaAsync(string anioPrograma);
        Task<List<EquipoRequeridoDto>> GetEquiposRequeridosAsync(string tipoReporte);

        /// <summary>Equipos de Oracle con su familia; sin filtro trae todos (lo usa el cruce del job).</summary>
        Task<List<EquipoFamiliaDto>> GetEquiposFamiliaAsync(string? familiaCodigo = null);
    }
}
