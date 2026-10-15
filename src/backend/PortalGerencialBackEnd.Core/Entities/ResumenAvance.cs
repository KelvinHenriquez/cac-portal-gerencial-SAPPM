namespace PortalGerencialBackEnd.Core.Entities
{
    /// <summary>
    /// Una fila del "Resumen de avance de reparación" (pedido del Taller, 14/10/2026): una familia de
    /// un programa de Oracle, con el grupo en que se muestra (Agrícola, Automotriz, Ferroviaria...) y
    /// el nombre con que sale. Lo configura la gente del área (ROL_PORTAL_CDS para Centro de Servicios).
    /// </summary>
    public class ResumenAvanceFila
    {
        public string Area { get; set; } = default!;
        public int ReporteId { get; set; }
        public string FamiliaCodigo { get; set; } = default!;
        public string Grupo { get; set; } = default!;
        public string Nombre { get; set; } = default!;
        public int Orden { get; set; }
        public string? ActualizadoPor { get; set; }
        public DateTime ActualizadoEn { get; set; }
    }

    /// <summary>Nota que alguien escribe en una fila del resumen para una semana ("2 pendiente revisión").</summary>
    public class ResumenAvanceNota
    {
        public string Area { get; set; } = default!;
        public int ReporteId { get; set; }
        public string FamiliaCodigo { get; set; } = default!;

        /// <summary>Último día de la semana del resumen: identifica la semana aunque los programas tengan calendarios distintos.</summary>
        public DateTime SemanaFin { get; set; }

        public string Texto { get; set; } = default!;
        public string? ActualizadoPor { get; set; }
        public DateTime ActualizadoEn { get; set; }
    }
}
