using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Dtos.Sistema;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Common;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Infrastructure.Jobs;

namespace PortalGerencialBackEnd.API.Controllers
{
    /// <summary>Frescura de los datos, bitácora del job y equipos sin clasificar (secciones 8.4, 10 y 11).</summary>
    [ApiController]
    [Route("api/sistema")]
    [Authorize]
    public class SistemaController : ControllerBase
    {
        private readonly ISistemaService _sistema;
        private readonly IProcesoActualizacionService _proceso;
        private readonly CoordinadorCorridas _coordinador;
        private readonly IUserContextService _usuario;

        public SistemaController(ISistemaService sistema, IProcesoActualizacionService proceso, CoordinadorCorridas coordinador, IUserContextService usuario)
        {
            _sistema = sistema;
            _proceso = proceso;
            _coordinador = coordinador;
            _usuario = usuario;
        }

        /// <summary>"Última actualización" del encabezado común.</summary>
        [HttpGet("actualizacion")]
        public async Task<ActionResult<EstadoActualizacionDto>> GetEstadoActualizacion() =>
            Ok(await _sistema.GetEstadoActualizacionAsync());

        [HttpGet("corridas")]
        public async Task<ActionResult<List<CorridaDto>>> GetCorridas([FromQuery] int cantidad = 20) =>
            Ok(await _sistema.GetCorridasAsync(cantidad));

        /// <summary>
        /// Corre el job ahora (extracción + recálculo) y devuelve su resultado. Solo lee de SAP y
        /// recalcula resultados propios del portal: no modifica nada en SAP ni en la estructura de
        /// Oracle (sección 10).
        /// </summary>
        /// <param name="desde">Volver a traer de SAP desde esa fecha (yyyy-MM-dd).</param>
        /// <remarks>Solo TI (ROL_PORTAL_ADMIN), sea "Actualizar ahora" o "Actualizar desde".</remarks>
        [HttpPost("corridas")]
        [Authorize(Roles = Roles.Actualizan)]
        public async Task<ActionResult<CorridaDto>> EjecutarCorrida([FromQuery] DateTime? desde = null)
        {
            // Sin el CancellationToken de la request: si el usuario cierra la pestaña, la corrida
            // termina igual en vez de quedar a medias.
            var (ejecutada, corrida) = await _coordinador.IntentarAsync(() => _proceso.EjecutarAsync(desde == null ? "MANUAL" : "RECARGA", desde));
            if (!ejecutada)
                throw new ConflictException("Ya hay una actualización en curso. Espere a que termine.");
            return Ok(corrida);
        }

        /// <summary>Alertas de frescura enviadas (o intentadas), de la más reciente a la más vieja.</summary>
        [HttpGet("alertas")]
        public async Task<ActionResult<List<AlertaDto>>> GetAlertas([FromQuery] int cantidad = 10) =>
            Ok(await _sistema.GetAlertasAsync(cantidad));

        /// <summary>
        /// Parámetros de cálculo (PARAMETRO_PGPM). Todos los ven; los de negocio los cambia
        /// ROL_PORTAL_PARAMETROS y los técnicos solo ROL_PORTAL_ADMIN.
        /// </summary>
        [HttpGet("parametros")]
        public async Task<ActionResult<ParametrosDto>> GetParametros() =>
            Ok(await _sistema.GetParametrosAsync(_usuario.Permisos));

        [HttpPut("parametros/{clave}")]
        public async Task<ActionResult<ParametroDto>> ActualizarParametro(string clave, [FromBody] ActualizarParametroRequest request) =>
            Ok(await _sistema.ActualizarParametroAsync(clave, request.Valor, _usuario.Usuario, _usuario.Permisos));

        [HttpGet("equipos-sin-clasificar")]
        public async Task<ActionResult<List<EquipoSinClasificarDto>>> GetEquiposSinClasificar() =>
            Ok(await _sistema.GetEquiposSinClasificarAsync());
    }
}
