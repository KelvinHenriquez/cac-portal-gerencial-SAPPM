namespace PortalGerencialBackEnd.Core.Dtos.Avance
{
    /// <summary>Pantalla Avance de Reparación (sección 8.3).</summary>
    public class ReporteAvanceDto
    {
        public string AnioPrograma { get; set; } = default!;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        /// <summary>Día al que corresponde lo "ejecutado": el del último cálculo.</summary>
        public DateTime? FechaCorte { get; set; }

        /// <summary>Fórmula con que se calculó el % real (provisional, ver AVANCE.FORMULA_REAL).</summary>
        public string FormulaReal { get; set; } = default!;

        /// <summary>Semanas de calendario (lunes a domingo) del programa completo.</summary>
        public List<SemanaAvanceDto> Semanas { get; set; } = new();

        public List<FilaAvanceDto> Familias { get; set; } = new();
    }

    public class SemanaAvanceDto
    {
        public DateTime Lunes { get; set; }
        public DateTime Domingo { get; set; }
        public int SemanaIso { get; set; }

        /// <summary>PASADA (terminó antes del corte), ACTUAL (contiene el corte) o FUTURA.</summary>
        public string Momento { get; set; } = default!;
    }

    public class FilaAvanceDto
    {
        public string FamiliaCodigo { get; set; } = default!;
        public string FamiliaNombre { get; set; } = default!;
        public int CantEquipo { get; set; }
        public DateTime FechaEstInicio { get; set; }
        public DateTime FechaEstFin { get; set; }

        /// <summary>% programado acumulado a la fecha de corte.</summary>
        public decimal TotalProgramadoPct { get; set; }

        /// <summary>% real ejecutado a la fecha de corte.</summary>
        public decimal TotalEjecutadoPct { get; set; }

        /// <summary>Ejecutado - programado: negativo = atraso, positivo = adelanto (en puntos).</summary>
        public decimal DiferenciaPct { get; set; }

        public int TotalOrdenes { get; set; }
        public int OrdenesCerradas { get; set; }
        public decimal HhPlan { get; set; }
        public decimal HhReal { get; set; }

        /// <summary>Una celda por cada SemanaAvanceDto del reporte (mismo orden).</summary>
        public List<CeldaAvanceDto> Semanas { get; set; } = new();
    }

    public class CeldaAvanceDto
    {
        /// <summary>False si la semana cae fuera de la ventana de reparación de la familia.</summary>
        public bool EnVentana { get; set; }

        /// <summary>Número de semana dentro del programa de la familia (curva de Oracle).</summary>
        public int? NoSemana { get; set; }

        public decimal? ProgPct { get; set; }

        /// <summary>Solo en semanas pasadas y la actual; null en las futuras.</summary>
        public decimal? RealPct { get; set; }
    }
}
