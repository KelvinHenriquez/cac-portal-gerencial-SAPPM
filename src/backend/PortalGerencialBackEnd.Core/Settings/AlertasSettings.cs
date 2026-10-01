namespace PortalGerencialBackEnd.Core.Settings
{
    /// <summary>
    /// Sección "Alertas" de appsettings / variables de entorno: a quién y cómo avisar cuando los
    /// datos llevan más de MONITOREO.HORAS_ALERTA sin actualizarse (sección 11).
    /// </summary>
    public class AlertasSettings
    {
        /// <summary>Correos separados por coma. Vacío = la alerta solo queda registrada en el portal.</summary>
        public string? Destinatarios { get; set; }

        /// <summary>Cada cuántos minutos se revisa la frescura.</summary>
        public int IntervaloMinutos { get; set; } = 60;

        /// <summary>Cada cuántas horas se repite la alerta mientras el problema siga.</summary>
        public int RepetirCadaHoras { get; set; } = 24;

        /// <summary>URL del portal para incluir en el correo.</summary>
        public string? UrlPortal { get; set; }

        public SmtpSettings Smtp { get; set; } = new();
    }

    public class SmtpSettings
    {
        /// <summary>Vacío = sin correo configurado.</summary>
        public string? Host { get; set; }

        public int Puerto { get; set; } = 587;
        public bool UsarSsl { get; set; } = true;
        public string? Usuario { get; set; }
        public string? Password { get; set; }
        public string Remitente { get; set; } = "portal-gerencial@cac.com.do";
    }
}
