using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalGerencialBackEnd.Core.Dtos.Avance;
using PortalGerencialBackEnd.Core.Interfaces.Services;

namespace PortalGerencialBackEnd.API.Controllers
{
    /// <summary>Avance de Reparación (sección 8.3). Solo lectura.</summary>
    [ApiController]
    [Route("api/avance")]
    [Authorize]
    public class AvanceController : ControllerBase
    {
        private readonly IReporteAvanceService _reporte;

        public AvanceController(IReporteAvanceService reporte)
        {
            _reporte = reporte;
        }

        [HttpGet("{anioPrograma}")]
        public async Task<ActionResult<ReporteAvanceDto>> GetAvance(string anioPrograma) =>
            Ok(await _reporte.GetAvanceAsync(anioPrograma));
    }
}
