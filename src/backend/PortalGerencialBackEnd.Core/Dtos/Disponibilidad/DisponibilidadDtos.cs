namespace PortalGerencialBackEnd.Core.Dtos.Disponibilidad
{
    /// <summary>Pantalla Disponibilidad de Equipos, como el reporte de Oracle.</summary>
    public class ReporteDisponibilidadDto
    {
        public int ReporteId { get; set; }
        public string Descripcion { get; set; } = default!;

        /// <summary>Rojo si &lt; UmbralRojo; naranja si &lt; UmbralNaranja; si no, negro.</summary>
        public decimal UmbralRojo { get; set; }
        public decimal UmbralNaranja { get; set; }

        /// <summary>Reglas vigentes (Sistema → Parámetros), para explicarlas al pie del reporte.</summary>
        public bool BaseEquipos { get; set; }
        public bool CorrectivasSegunAviso { get; set; }
        public string ClasesOrden { get; set; } = "";

        /// <summary>Instante de la foto de las columnas actuales: el de la última actualización exitosa.</summary>
        public DateTime? FechaCorte { get; set; }

        public List<SemanaColumnaDto> Semanas { get; set; } = new();

        /// <summary>Fila "Todas (Promedio)": total disponible / total requerido.</summary>
        public FilaDisponibilidadDto? Todas { get; set; }

        public List<FilaDisponibilidadDto> Filas { get; set; } = new();
    }

    public class SemanaColumnaDto
    {
        public int NoSemana { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        /// <summary>PASADA, ACTUAL o FUTURA respecto de la fecha de corte.</summary>
        public string Momento { get; set; } = default!;
    }

    public class FilaDisponibilidadDto
    {
        /// <summary>0 en la fila "Todas".</summary>
        public int AsignacionId { get; set; }

        public string Asignacion { get; set; } = default!;
        public bool Negrita { get; set; }

        /// <summary>Fila de grupo: varios equipos de distinta naturaleza que juntos resuelven una operación.</summary>
        public bool Grupo { get; set; }

        /// <summary>
        /// Fila de detalle de un grupo (las que le siguen hasta el próximo grupo): sus equipos ya
        /// están contados en la fila del grupo, así que no entra en "Todas".
        /// </summary>
        public bool EsMiembro { get; set; }

        /// <summary>Grupo que ya cuenta los equipos de esta fila (si EsMiembro).</summary>
        public string? EnGrupo { get; set; }

        public int CantEquipo { get; set; }
        public int EqRequerido { get; set; }
        public int EqDisponible { get; set; }
        public decimal DispRealPct { get; set; }
        public decimal PotencialPct { get; set; }
        public decimal DispCompPct { get; set; }
        public string Semaforo { get; set; } = default!;
        public List<CeldaSemanaDto> Semanas { get; set; } = new();
    }

    public class CeldaSemanaDto
    {
        public int NoSemana { get; set; }
        public decimal? DispRealPct { get; set; }
        public decimal? DispRealMinPct { get; set; }
        public decimal? PotencialPct { get; set; }
        public int DiasCalculados { get; set; }
        public string? Semaforo { get; set; }
    }

    /// <summary>Drill-down "Disponibilidad Semanal" de una asignación (pantalla de Oracle).</summary>
    public class DetalleSemanaDto
    {
        public int ReporteId { get; set; }
        public int AsignacionId { get; set; }
        public string Asignacion { get; set; } = default!;
        public int NoSemana { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal UmbralRojo { get; set; }
        public decimal UmbralNaranja { get; set; }
        public List<DiaDetalleDto> Dias { get; set; } = new();
        public DiaDetalleDto? Resumen { get; set; }
        public List<OrdenDetalleDto> Ordenes { get; set; } = new();
    }

    public class DiaDetalleDto
    {
        public DateTime? Fecha { get; set; }
        public int Dias { get; set; } = 1;
        public decimal DispRealPct { get; set; }
        public decimal DispCompPct { get; set; }
        public decimal HrsDia { get; set; }
        public decimal HrsOtPlanificada { get; set; }
        public decimal HrsOtPreventivo { get; set; }
        public decimal HrsOtCorrectiva { get; set; }
        public decimal HrsRealEfec { get; set; }
        public decimal HrsDiaDispComp { get; set; }
        public string Semaforo { get; set; } = default!;
    }

    /// <summary>Orden de la lista "Órdenes de Trabajo" del drill-down.</summary>
    public class OrdenDetalleDto
    {
        public string OrdenSapId { get; set; } = default!;
        public string ClaseOrden { get; set; } = default!;
        public string Tipo { get; set; } = default!;
        public string Ficha { get; set; } = default!;
        public string? EquipoDescripcion { get; set; }

        /// <summary>Desde cuándo deja el equipo fuera (creación, o inicio de la avería).</summary>
        public DateTime Desde { get; set; }

        /// <summary>Hasta cuándo (cierre técnico); null si sigue abierta.</summary>
        public DateTime? Hasta { get; set; }

        public string EstadoActual { get; set; } = default!;
        public string? Observacion { get; set; }
        public string? AvisoSapId { get; set; }
        /// <summary>Del aviso: true = se marcó que la máquina quedó parada; false = puede seguir trabajando; null = sin aviso.</summary>
        public bool? AvisoParada { get; set; }
        public DateTime? InicioAveria { get; set; }
        public DateTime? FinAveria { get; set; }
    }

    public class HoraDisponibilidadDto
    {
        public int Hora { get; set; }
        public int CantEquipo { get; set; }
        public int EqRequerido { get; set; }
        public int EqDisponible { get; set; }
        public decimal DispPct { get; set; }
        public decimal PotencialPct { get; set; }
    }

    /// <summary>Equipos de una asignación y su estado en la fecha de corte.</summary>
    public class EquiposAsignacionDto
    {
        public string Asignacion { get; set; } = default!;
        public DateTime Fecha { get; set; }
        public int CantRequerida { get; set; }
        public List<EquipoDetalleDto> Equipos { get; set; } = new();
    }

    public class EquipoDetalleDto
    {
        public string Ficha { get; set; } = default!;

        /// <summary>Código de equipo de Oracle (A29017-002).</summary>
        public string? EquipoCodigo { get; set; }

        public string Descripcion { get; set; } = default!;
        public string? EquipoSapId { get; set; }

        /// <summary>DISPONIBLE / FUERA_SERVICIO / INACTIVO / SIN_SAP.</summary>
        public string Estado { get; set; } = default!;

        public List<OrdenDetalleDto> Ordenes { get; set; } = new();

        /// <summary>Confiabilidad del equipo en el período pedido (MTTR y MTBF, en horas).</summary>
        public int Fallas { get; set; }
        public decimal? MttrHoras { get; set; }
        public decimal? MtbfHoras { get; set; }
    }

    /// <summary>MTTR y MTBF de una asignación (o de "Todas") en las semanas que se ven en pantalla.</summary>
    public class ConfiabilidadFilaDto
    {
        /// <summary>0 = "Todas".</summary>
        public int AsignacionId { get; set; }
        public int Equipos { get; set; }
        public int Fallas { get; set; }
        public decimal? MttrHoras { get; set; }
        public decimal? MtbfHoras { get; set; }
    }

    public class ConfiabilidadReporteDto
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public List<ConfiabilidadFilaDto> Filas { get; set; } = new();
        public ConfiabilidadFilaDto? Todas { get; set; }
    }
}
