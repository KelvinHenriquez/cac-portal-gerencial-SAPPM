namespace PortalGerencialBackEnd.Core.Entities.Calculo
{
    /// <summary>
    /// Avance semanal YA CALCULADO por familia (sección 3 y 6.2). Tabla CALC_AVANCE_SEMANAL_PGPM.
    /// Además de los % de la spec guarda los insumos (órdenes y horas), para poder auditar el
    /// número y cambiar la fórmula de "Total Ejec. %" (pendiente) sin perder trazabilidad.
    /// </summary>
    public class CalcAvanceSemanal
    {
        public string FamiliaCodigo { get; set; } = default!;
        public string AnioPrograma { get; set; } = default!;
        public int Semana { get; set; }
        public DateTime FechaInicioSemana { get; set; }
        public DateTime FechaFinSemana { get; set; }

        /// <summary>Acumulado de la curva planeada hasta esta semana (VW_CURVA_PLANEADA_PGPM).</summary>
        public decimal ProgPct { get; set; }

        /// <summary>Acumulado real derivado del staging SAP hasta esta semana.</summary>
        public decimal RealPct { get; set; }

        /// <summary>ProgPct - RealPct (atraso; negativo = adelantado).</summary>
        public decimal PendPct { get; set; }

        public int TotalOrdenes { get; set; }
        public int OrdenesCerradas { get; set; }
        public decimal HhPlan { get; set; }
        public decimal HhReal { get; set; }

        public string CorridaId { get; set; } = default!;
        public DateTime GeneradoEn { get; set; }
    }
}
