using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;

namespace PortalGerencialBackEnd.API.Controllers
{
    /// <summary>
    /// Estructura de negocio de Oracle (sección 4.1), en vivo y de solo lectura. Alimenta los
    /// selectores de las pantallas (tipo de reporte, programa) y el detalle por familia.
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

        [HttpGet("tipos-reporte")]
        public async Task<ActionResult<List<TipoReporteDto>>> GetTiposReporte() =>
            Ok(await _structureSource.GetTiposReporteAsync());

        [HttpGet("tipos-reporte/{tipoReporte}/umbrales")]
        public async Task<ActionResult<UmbralesDto>> GetUmbrales(string tipoReporte) =>
            Ok(await _structureSource.GetUmbralesAsync(tipoReporte)
               ?? throw new NotFoundException($"No existe el tipo de reporte '{tipoReporte}'."));

        [HttpGet("tipos-reporte/{tipoReporte}/equipos-requeridos")]
        public async Task<ActionResult<List<EquipoRequeridoDto>>> GetEquiposRequeridos(string tipoReporte) =>
            Ok(await _structureSource.GetEquiposRequeridosAsync(tipoReporte));

        [HttpGet("programas")]
        public async Task<ActionResult<List<ProgramaDto>>> GetProgramas() =>
            Ok(await _structureSource.GetProgramasAsync());

        [HttpGet("programas/{anioPrograma}/familias")]
        public async Task<ActionResult<List<FamiliaDto>>> GetFamilias(string anioPrograma) =>
            Ok(await _structureSource.GetFamiliasAsync(anioPrograma));

        [HttpGet("programas/{anioPrograma}/curva-planeada")]
        public async Task<ActionResult<List<CurvaSemanaDto>>> GetCurvaPlaneada(string anioPrograma) =>
            Ok(await _structureSource.GetCurvaPlaneadaAsync(anioPrograma));

        [HttpGet("familias/{familiaCodigo}/equipos")]
        public async Task<ActionResult<List<EquipoFamiliaDto>>> GetEquiposFamilia(string familiaCodigo) =>
            Ok(await _structureSource.GetEquiposFamiliaAsync(familiaCodigo));
    }
}
