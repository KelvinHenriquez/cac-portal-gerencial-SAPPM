using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalGerencialBackEnd.Core.Dtos.Permanencia;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Common;
using PortalGerencialBackEnd.Core.Interfaces.Services;

namespace PortalGerencialBackEnd.API.Controllers
{
    /// <summary>
    /// Reporte "Tiempo de Permanencia" (06/10/2026): órdenes correctivas, del aviso a la orden
    /// finalizada. Área → familia (tipo de equipo) → equipo → órdenes con sus pasos.
    /// Período por fecha de finalizada (desde/hasta, yyyy-MM-dd); por defecto, los últimos 3 meses.
    /// Cada usuario ve solo las áreas de sus roles (CDS, IND o las dos).
    /// </summary>
    [ApiController]
    [Route("api/permanencia")]
    [Authorize]
    public class PermanenciaController : ControllerBase
    {
        private readonly IReportePermanenciaService _servicio;
        private readonly IUserContextService _usuario;

        public PermanenciaController(IReportePermanenciaService servicio, IUserContextService usuario)
        {
            _servicio = servicio;
            _usuario = usuario;
        }

        [HttpGet("areas")]
        public async Task<ActionResult<List<AreaPermanenciaDto>>> GetAreas() =>
            Ok((await _servicio.GetAreasAsync()).Where(a => _usuario.Permisos.VeArea(a.Codigo)).ToList());

        [HttpGet("{area}")]
        public async Task<ActionResult<ResumenPermanenciaDto>> GetResumen(string area, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            ExigirArea(area);
            return Ok(await _servicio.GetResumenAsync(area, desde, hasta));
        }

        [HttpGet("{area}/familias/{tipoEquipo}")]
        public async Task<ActionResult<FamiliaPermanenciaDto>> GetFamilia(string area, string tipoEquipo, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        {
            ExigirArea(area);
            return Ok(await _servicio.GetFamiliaAsync(area, tipoEquipo, desde, hasta));
        }

        [HttpGet("equipos/{equipoSapId}")]
        public async Task<ActionResult<EquipoPermanenciaDto>> GetEquipo(string equipoSapId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta) =>
            Ok(await _servicio.GetEquipoAsync(equipoSapId, desde, hasta, _usuario.Permisos.Areas));

        private void ExigirArea(string area)
        {
            if (!_usuario.Permisos.VeArea(area))
                throw new ForbiddenException("Su usuario no tiene acceso a esta área.");
        }
    }
}
