using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Core.Entities;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Core.Settings;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Monitoreo
{
    /// <summary>
    /// Sección 11: revisa cada Alertas:IntervaloMinutos si los datos llevan más de
    /// MONITOREO.HORAS_ALERTA sin una corrida exitosa y, si es así, avisa por correo (una vez por
    /// problema, repitiendo cada Alertas:RepetirCadaHoras mientras siga). Cada intento queda en
    /// ALERTA_ENVIADA_PGPM, se haya podido enviar o no.
    /// </summary>
    public class MonitoreoFrescuraService : BackgroundService
    {
        public const string TipoDesactualizado = "DATOS_DESACTUALIZADOS";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly EnvioCorreo _correo;
        private readonly AlertasSettings _settings;
        private readonly TimeProvider _time;
        private readonly ILogger<MonitoreoFrescuraService> _logger;

        public MonitoreoFrescuraService(
            IServiceScopeFactory scopeFactory,
            EnvioCorreo correo,
            IOptions<AlertasSettings> settings,
            TimeProvider time,
            ILogger<MonitoreoFrescuraService> logger)
        {
            _scopeFactory = scopeFactory;
            _correo = correo;
            _settings = settings.Value;
            _time = time;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Después del arranque del job (20 s), para no alertar por la corrida que va a hacer.
                await Task.Delay(TimeSpan.FromSeconds(90), _time, stoppingToken);
                while (!stoppingToken.IsCancellationRequested)
                {
                    await RevisarAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, _settings.IntervaloMinutos)), _time, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // apagado normal
            }
        }

        public async Task RevisarAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sistema = scope.ServiceProvider.GetRequiredService<ISistemaService>();
                var db = scope.ServiceProvider.GetRequiredService<PortalGerencialDbContext>();

                var estado = await sistema.GetEstadoActualizacionAsync();
                var ultimaAlerta = await db.AlertasEnviadas
                    .Where(a => a.Tipo == TipoDesactualizado)
                    .MaxAsync(a => (DateTime?)a.Fecha, ct);
                var ahora = _time.GetLocalNow().DateTime;

                if (!ReglaAlertaFrescura.DebeAlertar(estado.Desactualizado, estado.UltimaActualizacion, ultimaAlerta, ahora,
                        TimeSpan.FromHours(Math.Max(1, _settings.RepetirCadaHoras))))
                    return;

                var destinatarios = (_settings.Destinatarios ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                var asunto = "Portal Gerencial SAP PM: los datos no se han actualizado";
                var alerta = new AlertaEnviada
                {
                    Tipo = TipoDesactualizado,
                    Fecha = ahora,
                    Destinatarios = destinatarios.Count == 0 ? null : string.Join(", ", destinatarios),
                    Asunto = asunto
                };

                if (destinatarios.Count == 0)
                    alerta.Error = "No hay destinatarios configurados (Alertas:Destinatarios).";
                else if (!_correo.Configurado)
                    alerta.Error = "El servidor de correo no está configurado (Alertas:Smtp:Host).";
                else
                {
                    try
                    {
                        await _correo.EnviarAsync(destinatarios, asunto, Cuerpo(estado), ct);
                        alerta.Enviada = true;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        alerta.Error = ex.GetBaseException().Message;
                    }
                }

                db.AlertasEnviadas.Add(alerta);
                await db.SaveChangesAsync(ct);

                if (alerta.Enviada)
                    _logger.LogWarning("Datos desactualizados: alerta enviada a {Destinatarios}.", alerta.Destinatarios);
                else
                    _logger.LogWarning("Datos desactualizados: la alerta no se pudo enviar ({Motivo}).", alerta.Error);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falló la revisión de frescura de datos.");
            }
        }

        private string Cuerpo(Core.Dtos.Sistema.EstadoActualizacionDto estado)
        {
            var es = CultureInfo.GetCultureInfo("es-DO");
            var sb = new StringBuilder();
            sb.AppendLine("Los reportes del Portal Gerencial SAP PM no se han actualizado con éxito en más de "
                          + $"{estado.HorasAlerta} horas.");
            sb.AppendLine();
            sb.AppendLine(estado.UltimaActualizacion is { } u
                ? $"Última actualización exitosa: {u.ToString("dd/MM/yyyy HH:mm", es)} (hace {estado.HorasDesdeUltimaActualizacion:0.#} horas)."
                : "No hay ninguna actualización exitosa registrada.");
            if (estado.UltimoErrorFecha is { } f)
            {
                sb.AppendLine($"Último error: {f.ToString("dd/MM/yyyy HH:mm", es)}");
                sb.AppendLine(estado.UltimoErrorMensaje);
            }
            sb.AppendLine();
            sb.AppendLine("Mientras tanto el portal sigue mostrando los últimos datos buenos, con el aviso de que no son de hoy.");
            if (!string.IsNullOrWhiteSpace(_settings.UrlPortal))
                sb.AppendLine($"Detalle de las corridas: {_settings.UrlPortal.TrimEnd('/')}/dashboard/sistema/actualizacion");
            sb.AppendLine();
            sb.AppendLine("Este aviso se repite cada " + _settings.RepetirCadaHoras + " horas mientras el problema continúe.");
            return sb.ToString();
        }
    }
}
