namespace PortalGerencialBackEnd.Core.Dtos.Avance
{
    /// <summary>Pantalla Avance de Órdenes de Trabajo, como el reporte de Oracle.</summary>
    public class ReporteAvanceDto
    {
        public int ReporteId { get; set; }
        public string Nombre { get; set; } = default!;
        public int PeriodoId { get; set; }
        public string PeriodoDescripcion { get; set; } = default!;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        /// <summary>Fecha a la que corresponde lo ejecutado: la de la última actualización.</summary>
        public DateTime? FechaCorte { get; set; }

        /// <summary>ORDENES (por defecto, lo que el área usa en la práctica) o ACTIVIDADES.</summary>
        public string Medida { get; set; } = default!;

        /// <summary>Solo con ACTIVIDADES: EJECUTADO (= PROGRAMADO + NO_PROGRAMADO), PROGRAMADO o NO_PROGRAMADO.</summary>
        public string RefPlan { get; set; } = default!;

        /// <summary>ACTIVIDADES o DIAS: con qué se arma el % programado de cada semana.</summary>
        public string BasePlan { get; set; } = default!;

        public List<SemanaAvanceDto> Semanas { get; set; } = new();
        public List<FilaAvanceDto> Familias { get; set; } = new();
    }

    public class SemanaAvanceDto
    {
        public int NoSemana { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        /// <summary>PASADA, ACTUAL (resaltada, como en Oracle) o FUTURA.</summary>
        public string Momento { get; set; } = default!;
    }

    public class FilaAvanceDto
    {
        public string FamiliaCodigo { get; set; } = default!;
        public string FamiliaNombre { get; set; } = default!;
        public int CantEquipo { get; set; }
        public DateTime FechaEstInicio { get; set; }
        public DateTime FechaEstFin { get; set; }

        // Columnas del reporte de Oracle (ver CalculadoraAvance.ColumnasALaFecha).
        public decimal TProg { get; set; }
        public decimal ProgCierre { get; set; }
        public decimal TotalEjec { get; set; }
        public decimal ProgAct { get; set; }
        public decimal PendAnt { get; set; }
        public decimal TPendR { get; set; }

        public int OrdenesTotal { get; set; }
        public int OrdenesCerradas { get; set; }
        public int ActTotal { get; set; }
        public int ActTerminadas { get; set; }

        /// <summary>Una celda por cada SemanaAvanceDto del reporte (mismo orden).</summary>
        public List<CeldaAvanceDto> Semanas { get; set; } = new();
    }

    public class CeldaAvanceDto
    {
        public int NoSemana { get; set; }

        /// <summary>False si la semana no está en el plan de la familia.</summary>
        public bool EnPlan { get; set; }

        public decimal? ProgPct { get; set; }

        /// <summary>Solo en semanas pasadas y la actual.</summary>
        public decimal? RealPct { get; set; }

        public decimal? ProgAcumPct { get; set; }
        public decimal? RealAcumPct { get; set; }
    }

    /// <summary>Drill-down "Avance Semanal" de una familia (pantalla de Oracle).</summary>
    public class DetalleSemanaAvanceDto
    {
        public string FamiliaCodigo { get; set; } = default!;
        public string FamiliaNombre { get; set; } = default!;
        public int NoSemana { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        // Resumen Semana
        public decimal Programado { get; set; }
        public decimal RealEjProg { get; set; }
        public decimal RealEjNoProg { get; set; }
        public decimal RealEjecutado { get; set; }
        public decimal RealOrdenes { get; set; }

        public List<DiaAvanceDto> Dias { get; set; } = new();
        public List<ActividadDto> Actividades { get; set; } = new();
        public List<OrdenCerradaDto> OrdenesCerradas { get; set; } = new();
    }

    public class DiaAvanceDto
    {
        public DateTime Fecha { get; set; }
        public int ActTerminadas { get; set; }
        public int OrdenesCerradas { get; set; }
    }

    public class ActividadDto
    {
        public string OrdenSapId { get; set; } = default!;
        public string Operacion { get; set; } = default!;
        public string Ficha { get; set; } = default!;
        public string? Texto { get; set; }
        public string? Sistema { get; set; }
        public string? Subsistema { get; set; }
        public string? Componente { get; set; }
        public bool Programada { get; set; }
        public DateTime? FechaTerminada { get; set; }
        public decimal Dias { get; set; }
    }

    public class OrdenCerradaDto
    {
        public string OrdenSapId { get; set; } = default!;
        public string Ficha { get; set; } = default!;
        public string? Descripcion { get; set; }
        public DateTime FechaCierre { get; set; }
    }

    /// <summary>"Análisis de Avance": por equipo y por sistema.</summary>
    public class AnalisisAvanceDto
    {
        public string FamiliaCodigo { get; set; } = default!;
        public string FamiliaNombre { get; set; } = default!;
        public AnalisisFilaDto Total { get; set; } = default!;
        public List<AnalisisEquipoDto> Equipos { get; set; } = new();
        public List<AnalisisFilaDto> Sistemas { get; set; } = new();
    }

    public class AnalisisFilaDto
    {
        public string Nombre { get; set; } = default!;
        public int ActTotal { get; set; }
        public int ActTerminadas { get; set; }
        public decimal DiasActProg { get; set; }
        public decimal DiasActTerm { get; set; }
        public decimal AvancePct { get; set; }
    }

    public class AnalisisEquipoDto : AnalisisFilaDto
    {
        public string Ficha { get; set; } = default!;
        public string OrdenSapId { get; set; } = default!;
        public string Estado { get; set; } = default!;
        public DateTime? FechaRealInicio { get; set; }
        public DateTime? FechaRealFin { get; set; }
    }
}
