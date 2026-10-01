using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;

namespace PortalGerencialBackEnd.API.Controllers
{
    /// <summary>
    /// Configuración de Oracle Forms, en vivo y de solo lectura. Alimenta los selectores de las
    /// pantallas (reporte de disponibilidad, período y reporte de avance).
    /// </summary>
    [ApiController]
    [Route("api/estructura")]
    [Authorize]
    public class EstructuraController : ControllerBase
    {
        private readonly IStructureSource _structureSource;

        public EstructuraController(IStructureSource structureSource)
        {
            _structureSource = structureSource;
        }

        [HttpGet("disponibilidad/reportes")]
        public async Task<ActionResult<List<ReporteResumenDto>>> GetReportesDisponibilidad() =>
            Ok(await _structureSource.GetReportesDisponibilidadAsync());

        [HttpGet("disponibilidad/reportes/{reporteId:int}")]
        public async Task<ActionResult<ConfigDisponibilidadDto>> GetConfigDisponibilidad(int reporteId) =>
            Ok(await _structureSource.GetConfigDisponibilidadAsync(reporteId)
               ?? throw new NotFoundException($"No existe el reporte de disponibilidad {reporteId}."));

        [HttpGet("avance/periodos")]
        public async Task<ActionResult<List<PeriodoDto>>> GetPeriodos() =>
            Ok(await _structureSource.GetPeriodosAsync());

        [HttpGet("avance/reportes/{reporteId:int}")]
        public async Task<ActionResult<ConfigAvanceDto>> GetConfigAvance(int reporteId) =>
            Ok(await _structureSource.GetConfigAvanceAsync(reporteId)
               ?? throw new NotFoundException($"No existe el reporte de avance {reporteId}."));

        [HttpGet("familias/{familiaCodigo}/equipos")]
        public async Task<ActionResult<List<EquipoFamiliaDto>>> GetEquiposFamilia(string familiaCodigo) =>
            Ok(await _structureSource.GetEquiposFamiliaAsync(familiaCodigo));
    }
}
