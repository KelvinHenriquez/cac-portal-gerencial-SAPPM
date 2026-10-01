namespace PortalGerencialBackEnd.Core.Entities.Calculo
{
    /// <summary>
    /// Disponibilidad de un día YA CALCULADA, por reporte y asignación (como el reporte de Oracle):
    /// en cada hora activa del reporte se cuentan los equipos disponibles y el día es el promedio de
    /// esas fotos. Además, las horas que los equipos pasaron en órdenes, por tipo.
    /// Tabla CALC_DISP_DIARIA_PGPM.
    /// </summary>
    public class CalcDisponibilidadDiaria
    {
        public int ReporteId { get; set; }
        public int AsignacionId { get; set; }
        public DateTime Fecha { get; set; }

        /// <summary>Equipos activos de la asignación (en Oracle y en SAP).</summary>
        public int CantEquipo { get; set; }

        public int EqRequerido { get; set; }

        /// <summary>Promedio de equipos disponibles en las horas medidas.</summary>
        public decimal EqDisponibleProm { get; set; }

        /// <summary>Disponibilidad Real %: promedio de las fotos horarias, cada una topada en 100.</summary>
        public decimal DispRealPct { get; set; }

        /// <summary>Potencial %: disponibles / requeridos sin tope (puede pasar de 100).</summary>
        public decimal PotencialPct { get; set; }

        /// <summary>Disp. Comp. %: meta configurada para la asignación.</summary>
        public decimal DispCompPct { get; set; }

        public int HorasMedidas { get; set; }

        /// <summary>Equipos × 24.</summary>
        public decimal HrsDia { get; set; }

        public decimal HrsOtPlanificada { get; set; }
        public decimal HrsOtPreventivo { get; set; }
        public decimal HrsOtCorrectiva { get; set; }

        /// <summary>Hrs al día − Hrs OT Correctiva (así salen los números del reporte de Oracle).</summary>
        public decimal HrsRealEfec { get; set; }

        /// <summary>Hrs al día × Disp. Comp. %.</summary>
        public decimal HrsDiaDispComp { get; set; }

        public string CorridaId { get; set; } = default!;
        public DateTime GeneradoEn { get; set; }
    }

    /// <summary>
    /// Semana del calendario del reporte (Sem. 1, 2, ...), promedio de sus días; horas sumadas.
    /// El color del semáforo se calcula al leer, con los umbrales vigentes. Tabla CALC_DISP_SEMANAL_PGPM.
    /// </summary>
    public class CalcDisponibilidadSemanal
    {
        public int ReporteId { get; set; }
        public int AsignacionId { get; set; }
        public int NoSemana { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int DiasCalculados { get; set; }
        public int CantEquipo { get; set; }
        public int EqRequerido { get; set; }
        public decimal EqDisponibleProm { get; set; }
        public decimal DispRealPct { get; set; }
        public decimal DispRealMinPct { get; set; }
        public decimal PotencialPct { get; set; }
        public decimal DispCompPct { get; set; }
        public decimal HrsDia { get; set; }
        public decimal HrsOtPlanificada { get; set; }
        public decimal HrsOtPreventivo { get; set; }
        public decimal HrsOtCorrectiva { get; set; }
        public decimal HrsRealEfec { get; set; }
        public decimal HrsDiaDispComp { get; set; }
        public string CorridaId { get; set; } = default!;
        public DateTime GeneradoEn { get; set; }
    }
}
