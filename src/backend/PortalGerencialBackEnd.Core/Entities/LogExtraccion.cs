namespace PortalGerencialBackEnd.Core.Entities
{
    /// <summary>
    /// Bitácora de cada paso de cada corrida del job (sección 3 y 11): de aquí sale "Última
    /// actualización" y la alerta de frescura. Tabla LOG_EXTRACCION_PGPM.
    /// </summary>
    public class LogExtraccion
    {
        /// <summary>
        /// GUID generado por la aplicación. Sin columnas IDENTITY ni DEFAULT seq.NEXTVAL: el Oracle de
        /// producción es 11g y esas dos cosas existen recién desde 12c.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Agrupa los pasos (extracciones y cálculos) de una misma corrida.</summary>
        public string CorridaId { get; set; } = default!;

        /// <summary>SAP_EQUIPOS / SAP_ORDENES / SAP_HISTORIAL / SAP_HORAS / CALC_DISPONIBILIDAD / CALC_AVANCE.</summary>
        public string Fuente { get; set; } = default!;

        public DateTime Inicio { get; set; }
        public DateTime? Fin { get; set; }

        /// <summary>EN_CURSO / OK / ERROR.</summary>
        public string Estado { get; set; } = default!;

        public int? RegistrosProcesados { get; set; }

        /// <summary>IV_FECHA_DESDE con que se llamó al módulo RFC (delta desde la última corrida OK).</summary>
        public DateTime? FechaCorteDesde { get; set; }

        /// <summary>LOCAL (SAP simulado) / PRODUCTION (RFC real) / MANUAL (lanzada desde el portal).</summary>
        public string? Origen { get; set; }

        public string? ErrorMensaje { get; set; }
    }
}
