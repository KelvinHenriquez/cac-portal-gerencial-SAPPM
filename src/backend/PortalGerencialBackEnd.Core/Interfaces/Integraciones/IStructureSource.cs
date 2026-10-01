using PortalGerencialBackEnd.Core.Dtos.Estructura;

namespace PortalGerencialBackEnd.Core.Interfaces.Integraciones
{
    /// <summary>
    /// Configuración que vive en Oracle Forms (sección 5), leída en vivo por las vistas _pgpm. Una
    /// sola implementación para local y producción: entre ambientes cambia a qué tablas apuntan las
    /// vistas, no el código.
    /// </summary>
    public interface IStructureSource
    {
        /// <summary>Reportes de disponibilidad activos, en el orden configurado.</summary>
        Task<List<ReporteResumenDto>> GetReportesDisponibilidadAsync();

        Task<ConfigDisponibilidadDto?> GetConfigDisponibilidadAsync(int reporteId);

        /// <summary>Configuración de todos los reportes de disponibilidad activos (la usa el job).</summary>
        Task<List<ConfigDisponibilidadDto>> GetConfigsDisponibilidadAsync();

        /// <summary>Períodos de reparación con sus reportes de avance, del más reciente al más viejo.</summary>
        Task<List<PeriodoDto>> GetPeriodosAsync();

        Task<ConfigAvanceDto?> GetConfigAvanceAsync(int reporteId);

        /// <summary>Configuración de todos los reportes de avance (la usan el job y el simulador).</summary>
        Task<List<ConfigAvanceDto>> GetConfigsAvanceAsync();

        /// <summary>Equipos de Oracle con su familia; sin filtro trae todos.</summary>
        Task<List<EquipoFamiliaDto>> GetEquiposFamiliaAsync(string? familiaCodigo = null);
    }
}
