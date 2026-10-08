using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;
using PortalGerencialBackEnd.Infrastructure.Services;

namespace PortalGerencialBackEnd.Infrastructure.Jobs
{
    /// <summary>
    /// Background Service del job (patrón "Background Services" de TPresto): una corrida diaria a
    /// la hora de JOB.HORA_CORRIDA_DIARIA, más una al arrancar si hoy todavía no hubo una exitosa
    /// (p. ej. el servidor estuvo apagado a esa hora). Se apaga con Job:Habilitado=false.
    /// </summary>
    public class ActualizacionProgramadaService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly CoordinadorCorridas _coordinador;
        private readonly TimeProvider _time;
        private readonly ILogger<ActualizacionProgramadaService> _logger;
        private readonly bool _habilitado;

        public ActualizacionProgramadaService(
            IServiceScopeFactory scopeFactory,
            CoordinadorCorridas coordinador,
            TimeProvider time,
            IConfiguration configuration,
            ILogger<ActualizacionProgramadaService> logger)
        {
            _scopeFactory = scopeFactory;
            _coordinador = coordinador;
            _time = time;
            _logger = logger;
            _habilitado = configuration.GetValue("Job:Habilitado", true);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_habilitado)
            {
                _logger.LogWarning("Job de actualización deshabilitado (Job:Habilitado=false).");
                return;
            }

            try
            {
                // Deja que la API termine de arrancar y de aplicar migraciones.
                await Task.Delay(TimeSpan.FromSeconds(20), _time, stoppingToken);

                if (!await HuboCorridaExitosaHoyAsync())
                    await CorrerAsync(stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var espera = await TiempoHastaProximaCorridaAsync();
                    _logger.LogInformation("Próxima corrida programada en {Horas:F1} h.", espera.TotalHours);
                    await Task.Delay(espera, _time, stoppingToken);
                    await CorrerAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // apagado normal
            }
        }

        private async Task CorrerAsync(CancellationToken ct)
        {
            try
            {
                var (ejecutada, _) = await _coordinador.IntentarAsync(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var proceso = scope.ServiceProvider.GetRequiredService<IProcesoActualizacionService>();
                    return await proceso.EjecutarAsync("PROGRAMADA", ct: ct);
                });

                if (!ejecutada)
                    _logger.LogInformation("Corrida programada omitida: ya había una en curso.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Los errores de cada paso ya quedan en la bitácora; esto es para fallas fuera de un
                // paso (p. ej. Oracle caído). El servicio sigue vivo para la próxima corrida.
                _logger.LogError(ex, "La corrida programada falló.");
            }
        }

        private async Task<bool> HuboCorridaExitosaHoyAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PortalGerencialDbContext>();
                var hoy = _time.GetLocalNow().Date;
                return await db.LogExtracciones.AnyAsync(l =>
                    l.Fuente == FuentesLog.CalcAvance && l.Estado == EstadosLog.Ok && l.Inicio >= hoy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo consultar la bitácora al arrancar.");
                return true; // ante la duda, esperar a la hora programada
            }
        }

        private async Task<TimeSpan> TiempoHastaProximaCorridaAsync()
        {
            var hora = new TimeSpan(5, 0, 0);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PortalGerencialDbContext>();
                hora = (await ParametrosPortal.CargarAsync(db)).Hora(ClavesParametro.HoraCorridaDiaria, hora);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo leer la hora de corrida; se usa {Hora}.", hora);
            }

            var ahora = _time.GetLocalNow().DateTime;
            return ProximaCorrida(ahora, hora) - ahora;
        }

        /// <summary>Próxima vez que toca la corrida diaria a la hora <paramref name="hora"/>.</summary>
        public static DateTime ProximaCorrida(DateTime ahora, TimeSpan hora)
        {
            var proxima = ahora.Date + hora;
            return proxima <= ahora ? proxima.AddDays(1) : proxima;
        }
    }
}
