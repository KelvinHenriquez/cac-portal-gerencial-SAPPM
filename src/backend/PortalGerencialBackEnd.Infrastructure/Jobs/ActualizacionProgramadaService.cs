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
    /// la hora de JOB.HORA_CORRIDA_DIARIA y, si esa falla, un reintento cada hora durante el día.
    /// Mira el reloj cada minuto en vez de dormir hasta la hora: una espera larga se atrasa si el
    /// servidor se pausa (respaldo de la VM, Docker/WSL dormido) y la corrida se perdía (10/10/2026).
    /// Si el servidor estuvo apagado a esa hora, corre al arrancar. Se apaga con Job:Habilitado=false.
    /// </summary>
    public class ActualizacionProgramadaService : BackgroundService
    {
        private static readonly TimeSpan Revision = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan Reintento = TimeSpan.FromHours(1);
        private static readonly TimeSpan RelecturaHora = TimeSpan.FromMinutes(15);

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

            var hora = new TimeSpan(5, 0, 0);
            DateTime? horaLeida = null, hechaPara = null, ultimoIntento = null;
            try
            {
                // Deja que la API termine de arrancar y de aplicar migraciones.
                await Task.Delay(TimeSpan.FromSeconds(20), _time, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var ahora = _time.GetLocalNow().DateTime;
                    if (horaLeida == null || ahora - horaLeida >= RelecturaHora)
                    {
                        var leida = await LeerHoraAsync(hora);
                        if (horaLeida == null || leida != hora)
                            _logger.LogInformation("Corrida diaria a las {Hora}; si falla, se reintenta cada hora.", leida.ToString(@"hh\:mm"));
                        hora = leida;
                        horaLeida = ahora;
                    }

                    var objetivo = ahora.Date + hora;
                    if (hechaPara != objetivo && TocaIntentar(ahora, hora, ultimoIntento))
                    {
                        if (await UltimaExitosaAsync() is { } ok && ok >= objetivo)
                            hechaPara = objetivo; // ya corrió hoy después de la hora (p. ej. a mano)
                        else
                        {
                            ultimoIntento = ahora;
                            await CorrerAsync(stoppingToken);
                        }
                    }

                    await Task.Delay(Revision, _time, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // apagado normal
            }
        }

        /// <summary>
        /// ¿Toca intentar la corrida del día? Ya pasó la hora y no se intentó desde entonces, o el
        /// último intento fue hace una hora o más. Si ya hay una exitosa lo decide el job con la bitácora.
        /// </summary>
        public static bool TocaIntentar(DateTime ahora, TimeSpan hora, DateTime? ultimoIntento)
        {
            var objetivo = ahora.Date + hora;
            if (ahora < objetivo) return false;
            return ultimoIntento == null || ultimoIntento < objetivo || ahora - ultimoIntento >= Reintento;
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
                // paso (p. ej. Oracle caído). Se reintenta en una hora.
                _logger.LogError(ex, "La corrida programada falló.");
            }
        }

        /// <summary>Inicio de la última corrida que terminó bien (su último paso, el avance, quedó OK).</summary>
        private async Task<DateTime?> UltimaExitosaAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PortalGerencialDbContext>();
                return await db.LogExtracciones
                    .Where(l => l.Fuente == FuentesLog.CalcAvance && l.Estado == EstadosLog.Ok)
                    .MaxAsync(l => (DateTime?)l.Inicio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo consultar la bitácora.");
                return null;
            }
        }

        private async Task<TimeSpan> LeerHoraAsync(TimeSpan actual)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PortalGerencialDbContext>();
                return (await ParametrosPortal.CargarAsync(db)).Hora(ClavesParametro.HoraCorridaDiaria, actual);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo leer la hora de corrida; se usa {Hora}.", actual);
                return actual;
            }
        }

        /// <summary>Próxima vez que toca la corrida diaria a la hora <paramref name="hora"/>.</summary>
        public static DateTime ProximaCorrida(DateTime ahora, TimeSpan hora)
        {
            var proxima = ahora.Date + hora;
            return proxima <= ahora ? proxima.AddDays(1) : proxima;
        }
    }
}
