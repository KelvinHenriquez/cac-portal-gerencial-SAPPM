namespace PortalGerencialBackEnd.Core.Entities.Calculo
{
    /// <summary>
    /// Disponibilidad diaria YA CALCULADA (sección 3 y 6.1); la UI nunca la calcula al vuelo.
    /// Tabla CALC_DISP_DIARIA_PGPM (la spec la llamaba calc_disponibilidad_diaria_pgpm: 31
    /// caracteres, uno más que el límite de nombres de Oracle anterior a 12.2).
    ///
    /// Diferencia con la spec: la llave incluye TIPO_REPORTE, porque los equipos requeridos
    /// dependen del tipo de reporte (VW_EQUIPOS_REQUERIDOS_PGPM) y una misma familia puede estar
    /// en dos tipos (p. ej. Tractores en CS-P30 y en Nivelación) con requeridos distintos.
    /// </summary>
    public class CalcDisponibilidadDiaria
    {
        public string TipoReporte { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public DateTime Fecha { get; set; }

        /// <summary>Equipos activos de la familia (VW_EQUIPOS_FAMILIA_PGPM).</summary>
        public int CantEquipo { get; set; }

        /// <summary>Suma de CANT_REQUERIDA de todas las asignaciones de la familia en ese tipo de reporte.</summary>
        public int EqRequerido { get; set; }

        /// <summary>Equipos con al menos una orden en estado "fuera de servicio" ese día.</summary>
        public int EqFueraServicio { get; set; }

        public int EqDisponible { get; set; }

        /// <summary>EqDisponible / EqRequerido, topado en 100 (decisión del 30/09/2026).</summary>
        public decimal DispRealPct { get; set; }

        /// <summary>Provisional: EqDisponible / CantEquipo. Pendiente la fórmula real de "Disp. Comp. %".</summary>
        public decimal DispPotencialPct { get; set; }

        public string CorridaId { get; set; } = default!;
        public DateTime GeneradoEn { get; set; }
    }
}
