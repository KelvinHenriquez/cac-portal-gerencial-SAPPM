using PortalGerencialBackEnd.Core.Dtos.Disponibilidad;

namespace PortalGerencialBackEnd.Core.Interfaces.Services
{
    /// <summary>
    /// Sección 5: lee resultados YA calculados (tablas calc_*_pgpm) y el staging; nunca llama a SAP.
    /// </summary>
    public interface IReporteDisponibilidadService
    {
        /// <param name="semanas">Cantidad de semanas ISO hacia atrás, terminando en la de la fecha de corte.</param>
        Task<ReporteDisponibilidadDto> GetDisponibilidadAsync(string tipoReporte, int semanas);

        Task<EquiposFamiliaDetalleDto> GetEquiposFamiliaAsync(string tipoReporte, string familiaCodigo);

        Task<DetalleSemanaDto> GetDetalleSemanaAsync(string tipoReporte, string familiaCodigo, int anioIso, int semanaIso);
    }
}
