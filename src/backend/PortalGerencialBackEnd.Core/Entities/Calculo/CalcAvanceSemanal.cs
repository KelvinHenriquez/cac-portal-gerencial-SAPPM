namespace PortalGerencialBackEnd.Core.Entities.Calculo
{
    /// <summary>
    /// Avance de UNA semana (no acumulado, como las columnas del reporte de Oracle) por reporte y
    /// familia, con las dos formas de medir el real: por órdenes cerradas y por actividades. Los
    /// acumulados, Prog. Cierre, Pend. Ant. y T. Pend. R. se arman al leer.
    /// Tabla CALC_AVANCE_SEMANAL_PGPM.
    /// </summary>
    public class CalcAvanceSemanal
    {
        public int ReporteId { get; set; }
        public string FamiliaCodigo { get; set; } = default!;
        public int NoSemana { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        /// <summary>% programado de esta semana (plan de Oracle / total del plan de la familia).</summary>
        public decimal ProgPct { get; set; }

        // --- real por órdenes: órdenes cerradas en la semana / total de órdenes del programa
        public int OrdenesTotal { get; set; }
        public int OrdenesCerradas { get; set; }
        public decimal RealOrdenesPct { get; set; }

        // --- real por actividades: días de actividades terminadas en la semana / días totales
        public int ActTotal { get; set; }
        public int ActTerminadas { get; set; }
        public decimal DiasActTotal { get; set; }
        public decimal DiasActTerminadas { get; set; }

        /// <summary>Real Ejecutado (programadas + no programadas).</summary>
        public decimal RealActPct { get; set; }

        /// <summary>Real Programado: actividades del plan original.</summary>
        public decimal RealActProgPct { get; set; }

        /// <summary>Real No Programado: actividades agregadas en la reparación.</summary>
        public decimal RealActNoProgPct { get; set; }

        public string CorridaId { get; set; } = default!;
        public DateTime GeneradoEn { get; set; }
    }
}
