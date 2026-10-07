namespace PortalGerencialBackEnd.Core.Entities.Calculo
{
    /// <summary>
    /// Tiempo de permanencia de una orden correctiva (reporte "Tiempo de Permanencia"): hitos y
    /// horas de cada paso, del aviso a la orden finalizada. Una fila por orden; se recalcula entera
    /// en cada corrida. El reporte agrega por área, tipo de equipo y equipo al leer.
    /// </summary>
    public class CalcPermanencia
    {
        public string OrdenSapId { get; set; } = default!;
        public string ClaseOrden { get; set; } = default!;
        public string? Descripcion { get; set; }
        public string? AvisoSapId { get; set; }

        public string EquipoSapId { get; set; } = default!;

        /// <summary>Clave de equipo: Ficha si la tiene; si no, el código SAP.</summary>
        public string ClaveEquipo { get; set; } = default!;
        public string? EquipoDescripcion { get; set; }

        /// <summary>Área según el grupo de planificación del equipo (CDS, IND).</summary>
        public string Area { get; set; } = default!;

        /// <summary>Tipo de equipo de SAP (EQART): la "familia" del reporte.</summary>
        public string TipoEquipo { get; set; } = default!;
        public string? TipoEquipoDesc { get; set; }

        public DateTime Inicio { get; set; }
        public DateTime? FechaAviso { get; set; }
        public DateTime FechaCreada { get; set; }
        public DateTime? FechaLiberada { get; set; }
        public DateTime? FechaEnProceso { get; set; }
        public DateTime? FechaFinalizada { get; set; }
        public DateTime? FechaCierreTecnico { get; set; }

        /// <summary>true = finalizada; false = en curso (las horas son la antigüedad al cálculo).</summary>
        public bool Terminada { get; set; }

        public decimal? HorasRespuesta { get; set; }
        public decimal? HorasLiberacion { get; set; }
        public decimal? HorasEsperaInicio { get; set; }
        public decimal? HorasEjecucion { get; set; }
        public decimal? HorasCierreAdm { get; set; }
        public decimal HorasPermanencia { get; set; }
        public decimal HorasEsperaRepuestos { get; set; }

        public string CorridaId { get; set; } = default!;
        public DateTime GeneradoEn { get; set; }
    }
}
