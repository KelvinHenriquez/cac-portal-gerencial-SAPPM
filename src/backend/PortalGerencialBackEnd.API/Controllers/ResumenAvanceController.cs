using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalGerencialBackEnd.Core.Dtos.ResumenAvance;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Common;
using PortalGerencialBackEnd.Core.Interfaces.Services;

namespace PortalGerencialBackEnd.API.Controllers
{
    /// <summary>
    /// "Resumen de avance de reparación" (14/10/2026): la lámina semanal por grupo y familia. La ve
    /// quien ve el área; la configura y anota la gente del área (o TI).
    /// </summary>
    [ApiController]
    [Route("api/resumen-avance/{area}")]
    [Authorize]
    public class ResumenAvanceController : ControllerBase
    {
        private readonly IResumenAvanceService _servicio;
        private readonly IUserContextService _usuario;

        public ResumenAvanceController(IResumenAvanceService servicio, IUserContextService usuario)
        {
            _servicio = servicio;
            _usuario = usuario;
        }

        /// <param name="semanaFin">Último día de la semana (yyyy-MM-dd); sin él, la semana actual.</param>
        /// <param name="medida">ORDENES (por defecto) o ACTIVIDADES.</param>
        [HttpGet]
        public async Task<ActionResult<ResumenAvanceDto>> Get(string area, [FromQuery] DateTime? semanaFin, [FromQuery] string medida = "ORDENES")
        {
            ExigirArea(area);
            return Ok(await _servicio.GetResumenAsync(area, semanaFin, medida, _usuario.Permisos));
        }

        [HttpGet("config")]
        public async Task<ActionResult<ConfigResumenDto>> GetConfig(string area)
        {
            ExigirArea(area);
            return Ok(await _servicio.GetConfigAsync(area));
        }

        [HttpPut("config")]
        public async Task<ActionResult<ConfigResumenDto>> GuardarConfig(string area, [FromBody] GuardarConfigResumenRequest request)
        {
            ExigirArea(area);
            return Ok(await _servicio.GuardarConfigAsync(area, request.Filas, _usuario.Usuario, _usuario.Permisos));
        }

        [HttpPut("notas")]
        public async Task<IActionResult> GuardarNota(string area, [FromBody] GuardarNotaResumenRequest request)
        {
            ExigirArea(area);
            await _servicio.GuardarNotaAsync(area, request, _usuario.Usuario, _usuario.Permisos);
            return NoContent();
        }

        private void ExigirArea(string area)
        {
            if (!_usuario.Permisos.VeArea(area))
                throw new ForbiddenException("Su usuario no tiene acceso a esta área.");
        }
    }
}
