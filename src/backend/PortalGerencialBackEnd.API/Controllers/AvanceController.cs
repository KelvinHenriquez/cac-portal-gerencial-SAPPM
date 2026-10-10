using PortalGerencialBackEnd.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalGerencialBackEnd.Core.Dtos.Avance;
using PortalGerencialBackEnd.Core.Interfaces.Services;

namespace PortalGerencialBackEnd.API.Controllers
{
    /// <summary>Avance de Órdenes de Trabajo (como el reporte de Oracle). Solo lectura.</summary>
    [ApiController]
    [Route("api/avance/{reporteId:int}")]
    // Reportes de Centro de Servicios: Disponibilidad y Avance se configuran por Ficha en Oracle.
    [Authorize(Roles = Roles.VenCentroServicios)]
    public class AvanceController : ControllerBase
    {
        private readonly IReporteAvanceService _reporte;

        public AvanceController(IReporteAvanceService reporte)
        {
            _reporte = reporte;
        }

        /// <param name="medida">ORDENES (por defecto) o ACTIVIDADES.</param>
        /// <param name="refPlan">Con ACTIVIDADES: EJECUTADO (por defecto), PROGRAMADO o NO_PROGRAMADO.</param>
        [HttpGet]
        public async Task<ActionResult<ReporteAvanceDto>> GetReporte(int reporteId, [FromQuery] string medida = "ORDENES", [FromQuery] string refPlan = "EJECUTADO") =>
            Ok(await _reporte.GetReporteAsync(reporteId, medida, refPlan));

        /// <summary>Drill-down "Avance Semanal": resumen, días, actividades terminadas y órdenes cerradas.</summary>
        [HttpGet("familias/{familiaCodigo}/semanas/{noSemana:int}")]
        public async Task<ActionResult<DetalleSemanaAvanceDto>> GetDetalleSemana(int reporteId, string familiaCodigo, int noSemana) =>
            Ok(await _reporte.GetDetalleSemanaAsync(reporteId, familiaCodigo, noSemana));

        /// <summary>"Análisis de Avance": por equipo y por sistema.</summary>
        [HttpGet("familias/{familiaCodigo}/analisis")]
        public async Task<ActionResult<AnalisisAvanceDto>> GetAnalisis(int reporteId, string familiaCodigo) =>
            Ok(await _reporte.GetAnalisisAsync(reporteId, familiaCodigo));
    }
}
