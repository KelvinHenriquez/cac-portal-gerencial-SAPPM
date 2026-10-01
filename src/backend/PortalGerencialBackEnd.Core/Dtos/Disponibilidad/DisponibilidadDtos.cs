namespace PortalGerencialBackEnd.Core.Dtos.Disponibilidad
{
    /// <summary>Pantalla Disponibilidad de Maquinaria (sección 8.2).</summary>
    public class ReporteDisponibilidadDto
    {
        public string TipoReporte { get; set; } = default!;
        public string Descripcion { get; set; } = default!;
        public decimal UmbralRojo { get; set; }
        public decimal UmbralNaranja { get; set; }

        /// <summary>Último día calculado: las columnas "actuales" (Eq. Disponible, %) son de este día.</summary>
        public DateTime? FechaCorte { get; set; }

        public List<SemanaColumnaDto> Semanas { get; set; } = new();
        public List<FilaDisponibilidadDto> Familias { get; set; } = new();

        /// <summary>Fila "Todas (Promedio)": cantidades sumadas; porcentajes = promedio simple de las familias.</summary>
        public FilaDisponibilidadDto? Promedio { get; set; }
    }

    public class SemanaColumnaDto
    {
        public int AnioIso { get; set; }
        public int SemanaIso { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }

    public class FilaDisponibilidadDto
    {
        public string FamiliaCodigo { get; set; } = default!;
        public string FamiliaNombre { get; set; } = default!;
        public int CantEquipo { get; set; }
        public int EqRequerido { get; set; }
        public int EqDisponible { get; set; }
        public int EqFueraServicio { get; set; }
        public decimal DispRealPct { get; set; }
        public decimal DispPotencialPct { get; set; }
        public string Semaforo { get; set; } = default!;
        public List<CeldaSemanaDto> Semanas { get; set; } = new();
    }

    public class CeldaSemanaDto
    {
        public int AnioIso { get; set; }
        public int SemanaIso { get; set; }

        /// <summary>Null = sin dato calculado para esa semana.</summary>
        public decimal? DispRealPct { get; set; }

        public decimal? DispRealMinPct { get; set; }
        public decimal? EqDisponibleProm { get; set; }
        public int DiasCalculados { get; set; }
        public string? Semaforo { get; set; }
    }

    /// <summary>Drill-down por familia: equipos individuales (sección 8.2).</summary>
    public class EquiposFamiliaDetalleDto
    {
        public string FamiliaCodigo { get; set; } = default!;
        public string FamiliaNombre { get; set; } = default!;
        public DateTime Fecha { get; set; }
        public List<RequeridoAsignacionDto> Requeridos { get; set; } = new();
        public List<EquipoDetalleDto> Equipos { get; set; } = new();
    }

    public class RequeridoAsignacionDto
    {
        public string Asignacion { get; set; } = default!;
        public int CantAsignada { get; set; }
        public int CantRequerida { get; set; }
    }

    public class EquipoDetalleDto
    {
        /// <summary>Código legado = ficha del equipo en Oracle.</summary>
        public string CodigoLegado { get; set; } = default!;

        public string Descripcion { get; set; } = default!;
        public string? EquipoSapId { get; set; }
        public bool Activo { get; set; }

        /// <summary>DISPONIBLE / FUERA_SERVICIO / INACTIVO / SIN_SAP (está en Oracle pero no aparece en SAP).</summary>
        public string Estado { get; set; } = default!;

        /// <summary>Órdenes que lo tienen fuera de servicio en la fecha.</summary>
        public List<OrdenResumenDto> Ordenes { get; set; } = new();
    }

    public class OrdenResumenDto
    {
        public string OrdenSapId { get; set; } = default!;
        public string ClaseOrden { get; set; } = default!;
        public string EstadoOrden { get; set; } = default!;
        public string? Descripcion { get; set; }
        public string CodigoLegado { get; set; } = default!;
        public string? EquipoDescripcion { get; set; }
        public DateTime? FechaInicioReal { get; set; }
        public DateTime? FechaFinReal { get; set; }
    }

    /// <summary>Drill-down por semana: detalle diario de una familia con las órdenes de cada día (sección 8.2).</summary>
    public class DetalleSemanaDto
    {
        public string TipoReporte { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public string FamiliaNombre { get; set; } = default!;
        public int AnioIso { get; set; }
        public int SemanaIso { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public List<DiaDisponibilidadDto> Dias { get; set; } = new();
    }

    public class DiaDisponibilidadDto
    {
        public DateTime Fecha { get; set; }
        public int CantEquipo { get; set; }
        public int EqRequerido { get; set; }
        public int EqFueraServicio { get; set; }
        public int EqDisponible { get; set; }
        public decimal DispRealPct { get; set; }
        public string Semaforo { get; set; } = default!;
        public List<OrdenResumenDto> Ordenes { get; set; } = new();
    }
}
