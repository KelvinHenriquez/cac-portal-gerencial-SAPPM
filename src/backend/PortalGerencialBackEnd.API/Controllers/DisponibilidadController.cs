using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalGerencialBackEnd.Core.Dtos.Disponibilidad;
using PortalGerencialBackEnd.Core.Interfaces.Services;

namespace PortalGerencialBackEnd.API.Controllers
{
    /// <summary>Disponibilidad de Maquinaria (sección 8.2). Solo lectura.</summary>
    [ApiController]
    [Route("api/disponibilidad")]
    [Authorize]
    public class DisponibilidadController : ControllerBase
    {
        private readonly IReporteDisponibilidadService _reporte;

        public DisponibilidadController(IReporteDisponibilidadService reporte)
        {
            _reporte = reporte;
        }

        /// <summary>Tabla por familia con columnas semanales y semáforo.</summary>
        [HttpGet("{tipoReporte}")]
        public async Task<ActionResult<ReporteDisponibilidadDto>> GetReporte(string tipoReporte, [FromQuery] int semanas = 8) =>
            Ok(await _reporte.GetDisponibilidadAsync(tipoReporte, semanas));

        /// <summary>Drill-down: equipos de una familia y su estado a la fecha de corte.</summary>
        [HttpGet("{tipoReporte}/familias/{familiaCodigo}/equipos")]
        public async Task<ActionResult<EquiposFamiliaDetalleDto>> GetEquipos(string tipoReporte, string familiaCodigo) =>
            Ok(await _reporte.GetEquiposFamiliaAsync(tipoReporte, familiaCodigo));

        /// <summary>Drill-down: detalle diario de una semana con las órdenes de cada día.</summary>
        [HttpGet("{tipoReporte}/familias/{familiaCodigo}/semanas/{anioIso:int}/{semanaIso:int}")]
        public async Task<ActionResult<DetalleSemanaDto>> GetDetalleSemana(string tipoReporte, string familiaCodigo, int anioIso, int semanaIso) =>
            Ok(await _reporte.GetDetalleSemanaAsync(tipoReporte, familiaCodigo, anioIso, semanaIso));
    }
}
