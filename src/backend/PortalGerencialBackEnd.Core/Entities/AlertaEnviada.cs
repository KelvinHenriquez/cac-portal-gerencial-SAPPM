namespace PortalGerencialBackEnd.Core.Entities
{
    /// <summary>
    /// Cada intento de alerta de frescura (sección 11), enviada o no. Sirve para no repetir la misma
    /// alerta cada hora y para mostrar en el portal cuándo se avisó. Tabla ALERTA_ENVIADA_PGPM.
    /// </summary>
    public class AlertaEnviada
    {
        /// <summary>GUID generado por la aplicación (Oracle 11g no tiene IDENTITY).</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>DATOS_DESACTUALIZADOS (por ahora el único tipo).</summary>
        public string Tipo { get; set; } = default!;

        public DateTime Fecha { get; set; }
        public string? Destinatarios { get; set; }
        public string Asunto { get; set; } = default!;

        /// <summary>True si el servidor de correo la aceptó.</summary>
        public bool Enviada { get; set; }

        /// <summary>Por qué no se envió (SMTP sin configurar, error del servidor, ...).</summary>
        public string? Error { get; set; }
    }
}
