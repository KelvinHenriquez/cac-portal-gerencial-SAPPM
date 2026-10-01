namespace PortalGerencialBackEnd.Core.Entities.Sap
{
    /// <summary>
    /// Espejo de las órdenes de mantenimiento de SAP (AUFK/AFIH/AFKO/JEST, Anexo A.2), solo con
    /// las columnas que usan disponibilidad y avance. Tabla STG_SAP_ORDEN_PGPM.
    /// </summary>
    public class StgSapOrden
    {
        public string OrdenSapId { get; set; } = default!;

        /// <summary>ZC01, ZC03 (predictiva de reparación, la que cuenta para Avance), etc.</summary>
        public string ClaseOrden { get; set; } = default!;

        public string? EquipoSapId { get; set; }

        /// <summary>Texto breve de la orden — para el detalle de órdenes del día (sección 8.2).</summary>
        public string? Descripcion { get; set; }

        public string? PuestoTrabajo { get; set; }

        /// <summary>Estado actual (Pendiente / Liberado / En Proceso / Cerrado técnicamente / ...).</summary>
        public string EstadoOrden { get; set; } = default!;

        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaInicioPlan { get; set; }
        public DateTime? FechaFinPlan { get; set; }
        public DateTime? FechaInicioReal { get; set; }
        public DateTime? FechaFinReal { get; set; }
        public DateTime ActualizadoEn { get; set; }

        public StgSapEquipo? Equipo { get; set; }
        public ICollection<StgSapOperacionHoras> Operaciones { get; set; } = new List<StgSapOperacionHoras>();
    }
}
