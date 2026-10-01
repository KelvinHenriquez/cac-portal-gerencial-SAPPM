namespace PortalGerencialBackEnd.Core.Dtos.Sap
{
    // Lo que devuelven los módulos RFC del Anexo A, ya convertido a tipos .NET. El resto del
    // backend solo conoce estos DTOs: no sabe si vienen de SAP real o del simulador local.

    /// <summary>Z_PM_TALLERES_EQUIPOS (EQUI).</summary>
    public class EquipoSapDto
    {
        public string EquipoSapId { get; set; } = default!;
        public string? CodigoLegado { get; set; }
        public string? Descripcion { get; set; }
        public string? PuestoTrabajo { get; set; }
    }

    /// <summary>Z_PM_TALLERES_ORDENES (AUFK/AFIH/AFKO/JEST).</summary>
    public class OrdenSapDto
    {
        public string OrdenSapId { get; set; } = default!;
        public string ClaseOrden { get; set; } = default!;
        public string? EquipoSapId { get; set; }
        public string? Descripcion { get; set; }
        public string? PuestoTrabajo { get; set; }
        public string EstadoOrden { get; set; } = default!;
        public DateTime? FechaCreacion { get; set; }
        public DateTime? FechaInicioPlan { get; set; }
        public DateTime? FechaFinPlan { get; set; }
        public DateTime? FechaInicioReal { get; set; }
        public DateTime? FechaFinReal { get; set; }

        /// <summary>AUFK-AEDAT: última modificación, la que filtra el delta.</summary>
        public DateTime FechaModificacion { get; set; }
    }

    /// <summary>Z_PM_TALLERES_CONFIRMACIONES (AFVC + AFRU), una fila por operación.</summary>
    public class OperacionHorasDto
    {
        public string OrdenSapId { get; set; } = default!;
        public string Operacion { get; set; } = default!;
        public decimal TrabajoPlanHh { get; set; }
        public decimal TrabajoRealHh { get; set; }
    }

    /// <summary>Z_PM_TALLERES_HISTORIAL_ESTADO (JCDS).</summary>
    public class HistorialEstadoDto
    {
        public string OrdenSapId { get; set; } = default!;
        public string EstadoCodigo { get; set; } = default!;
        public string? EstadoTexto { get; set; }
        public string NumeroCambio { get; set; } = default!;
        public DateTime FechaHoraCambio { get; set; }
        public string Indicador { get; set; } = default!;
    }
}
