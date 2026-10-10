using PortalGerencialBackEnd.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalGerencialBackEnd.Core.Dtos.Disponibilidad;
using PortalGerencialBackEnd.Core.Interfaces.Services;

namespace PortalGerencialBackEnd.API.Controllers
{
    /// <summary>Disponibilidad de Equipos (como el reporte de Oracle). Solo lectura.</summary>
    [ApiController]
    [Route("api/disponibilidad/{reporteId:int}")]
    // Reportes de Centro de Servicios: Disponibilidad y Avance se configuran por Ficha en Oracle.
    [Authorize(Roles = Roles.VenCentroServicios)]
    public class DisponibilidadController : ControllerBase
    {
        private readonly IReporteDisponibilidadService _reporte;

        public DisponibilidadController(IReporteDisponibilidadService reporte)
        {
            _reporte = reporte;
        }

        /// <summary>Tabla por asignación con la foto actual, las semanas del reporte y la fila "Todas".</summary>
        [HttpGet]
        public async Task<ActionResult<ReporteDisponibilidadDto>> GetReporte(int reporteId) =>
            Ok(await _reporte.GetReporteAsync(reporteId));

        /// <summary>Drill-down "Disponibilidad Semanal": días, resumen y órdenes de la semana.</summary>
        [HttpGet("asignaciones/{asignacionId:int}/semanas/{noSemana:int}")]
        public async Task<ActionResult<DetalleSemanaDto>> GetDetalleSemana(int reporteId, int asignacionId, int noSemana) =>
            Ok(await _reporte.GetDetalleSemanaAsync(reporteId, asignacionId, noSemana));

        /// <summary>Disponibilidad hora por hora de un día (fecha yyyy-MM-dd).</summary>
        [HttpGet("asignaciones/{asignacionId:int}/dias/{fecha:datetime}/horas")]
        public async Task<ActionResult<List<HoraDisponibilidadDto>>> GetHoras(int reporteId, int asignacionId, DateTime fecha) =>
            Ok(await _reporte.GetHorasAsync(reporteId, asignacionId, fecha));

        /// <summary>Equipos de la asignación y su estado en la última actualización.</summary>
        [HttpGet("asignaciones/{asignacionId:int}/equipos")]
        public async Task<ActionResult<EquiposAsignacionDto>> GetEquipos(int reporteId, int asignacionId) =>
            Ok(await _reporte.GetEquiposAsync(reporteId, asignacionId));
    }
}
