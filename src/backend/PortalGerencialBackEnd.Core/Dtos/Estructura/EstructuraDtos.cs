namespace PortalGerencialBackEnd.Core.Dtos.Estructura
{
    public class TipoReporteDto
    {
        public string TipoReporte { get; set; } = default!;
        public string Descripcion { get; set; } = default!;
    }

    /// <summary>Disponibilidad &lt; UmbralRojo = rojo; &lt; UmbralNaranja = naranja; si no, verde.</summary>
    public class UmbralesDto
    {
        public string TipoReporte { get; set; } = default!;
        public decimal UmbralRojo { get; set; }
        public decimal UmbralNaranja { get; set; }
    }

    public class ProgramaDto
    {
        public string AnioPrograma { get; set; } = default!;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int CantFamilias { get; set; }
    }

    public class FamiliaDto
    {
        public string AnioPrograma { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public string FamiliaNombre { get; set; } = default!;
        public DateTime FechaEstInicio { get; set; }
        public DateTime FechaEstFin { get; set; }
        public int CantEquipo { get; set; }
    }

    public class CurvaSemanaDto
    {
        public string AnioPrograma { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public int Mes { get; set; }
        public int NoSemana { get; set; }
        public DateTime FechaInicioSemana { get; set; }
        public DateTime FechaFinSemana { get; set; }
        public decimal ActividadPct { get; set; }
        public int Dias { get; set; }
    }

    public class EquipoRequeridoDto
    {
        public string TipoReporte { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public string Asignacion { get; set; } = default!;
        public int CantAsignada { get; set; }
        public int CantRequerida { get; set; }
    }

    public class EquipoFamiliaDto
    {
        public string CodigoLegado { get; set; } = default!;
        public string Descripcion { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public bool Activo { get; set; }
    }
}
