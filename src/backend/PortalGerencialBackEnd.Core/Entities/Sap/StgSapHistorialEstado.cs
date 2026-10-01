namespace PortalGerencialBackEnd.Core.Entities.Sap
{
    /// <summary>
    /// Cada cambio de estado de una orden, con fecha y hora exactas (JCDS, Anexo A.1/A.3). No
    /// estaba en el modelo de la sección 3, pero sin esta tabla no hay dónde guardar lo que trae
    /// Z_PM_TALLERES_HISTORIAL_ESTADO — y es lo que permite reconstruir el estado de un equipo
    /// en cualquier instante pasado. Tabla STG_SAP_HIST_ESTADO_PGPM.
    /// </summary>
    public class StgSapHistorialEstado
    {
        public string OrdenSapId { get; set; } = default!;

        /// <summary>Código interno del estado (JCDS-STAT, p. ej. I0002 = Liberado).</summary>
        public string EstadoCodigo { get; set; } = default!;

        /// <summary>Número de cambio (JCDS-CHGNR): distingue activaciones repetidas del mismo estado.</summary>
        public string NumeroCambio { get; set; } = default!;

        /// <summary>Texto corto del estado (TJ30T-TXT04 / TJ02T).</summary>
        public string? EstadoTexto { get; set; }

        /// <summary>UDATE + UTIME.</summary>
        public DateTime FechaHoraCambio { get; set; }

        /// <summary>'I' = el estado se activó; 'D' = se desactivó (JCDS-CHIND).</summary>
        public string Indicador { get; set; } = default!;

        public DateTime ActualizadoEn { get; set; }
    }
}
