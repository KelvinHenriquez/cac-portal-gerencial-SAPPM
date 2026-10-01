using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using PortalGerencialBackEnd.Core.Settings;

namespace PortalGerencialBackEnd.Infrastructure.Monitoreo
{
    /// <summary>Correo simple por SMTP para las alertas. Sin Alertas:Smtp:Host configurado no envía.</summary>
    public class EnvioCorreo
    {
        private readonly SmtpSettings _smtp;

        public EnvioCorreo(IOptions<AlertasSettings> settings)
        {
            _smtp = settings.Value.Smtp;
        }

        public bool Configurado => !string.IsNullOrWhiteSpace(_smtp.Host);

        public async Task EnviarAsync(IEnumerable<string> destinatarios, string asunto, string cuerpo, CancellationToken ct)
        {
            if (!Configurado)
                throw new InvalidOperationException("El servidor de correo (Alertas:Smtp:Host) no está configurado.");

            using var mensaje = new MailMessage
            {
                From = new MailAddress(_smtp.Remitente, "Portal Gerencial SAP PM"),
                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = false
            };
            foreach (var d in destinatarios)
                mensaje.To.Add(d);

            using var cliente = new SmtpClient(_smtp.Host, _smtp.Puerto)
            {
                EnableSsl = _smtp.UsarSsl,
                Credentials = string.IsNullOrWhiteSpace(_smtp.Usuario) ? null : new NetworkCredential(_smtp.Usuario, _smtp.Password),
                Timeout = 30_000
            };
            await cliente.SendMailAsync(mensaje, ct);
        }
    }
}
