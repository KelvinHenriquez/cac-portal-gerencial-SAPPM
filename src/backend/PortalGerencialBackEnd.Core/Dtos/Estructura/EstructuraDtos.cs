namespace PortalGerencialBackEnd.Core.Dtos.Estructura
{
    public class EquipoFamiliaDto
    {
        public string CodigoLegado { get; set; } = default!;
        public string? EquipoCodigo { get; set; }
        public string Descripcion { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public bool Activo { get; set; }
    }

    // ------------------------------------------------------------ disponibilidad

    public class ReporteResumenDto
    {
        public int ReporteId { get; set; }
        public string Descripcion { get; set; } = default!;
    }

    /// <summary>Configuración completa de un reporte de disponibilidad (pantalla MA_CONFIGURACION_DISP).</summary>
    public class ConfigDisponibilidadDto
    {
        public int ReporteId { get; set; }
        public int Orden { get; set; }
        public string Descripcion { get; set; } = default!;
        public decimal UmbralRojo { get; set; }
        public decimal UmbralNaranja { get; set; }
        public bool Activo { get; set; }
        public List<SemanaCalendarioDto> Semanas { get; set; } = new();

        /// <summary>Horas del día (0-23) en que se toma la foto de disponibilidad.</summary>
        public List<int> HorasActivas { get; set; } = new();

        public List<AsignacionDto> Asignaciones { get; set; } = new();
    }

    public class SemanaCalendarioDto
    {
        public int NoSemana { get; set; }
        public int Mes { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }

    public class AsignacionDto
    {
        public int AsignacionId { get; set; }
        public int Orden { get; set; }
        public string Nombre { get; set; } = default!;
        public int CantAsignada { get; set; }
        public int CantRequerida { get; set; }
        public decimal DispCompPct { get; set; }
        public bool Negrita { get; set; }
        public bool Grupo { get; set; }

        /// <summary>Códigos legados de los equipos de la asignación.</summary>
        public List<string> Equipos { get; set; } = new();
    }

    // ------------------------------------------------------------ avance

    public class PeriodoDto
    {
        public int PeriodoId { get; set; }
        public string Descripcion { get; set; } = default!;
        public List<ReporteResumenDto> Reportes { get; set; } = new();
    }

    /// <summary>Configuración de un reporte de avance (pantalla MA_CONFIG_AVANCE).</summary>
    public class ConfigAvanceDto
    {
        public int ReporteId { get; set; }
        public int PeriodoId { get; set; }
        public string PeriodoDescripcion { get; set; } = default!;
        public string Nombre { get; set; } = default!;
        public string TipoOt { get; set; } = default!;
        public string FechaRef { get; set; } = default!;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public List<FamiliaAvanceDto> Familias { get; set; } = new();
    }

    public class FamiliaAvanceDto
    {
        public int Orden { get; set; }
        public string FamiliaCodigo { get; set; } = default!;
        public string FamiliaNombre { get; set; } = default!;
        public DateTime FechaEstInicio { get; set; }
        public DateTime FechaEstFin { get; set; }
        public int CantEquipo { get; set; }
        public List<SemanaPlanDto> Semanas { get; set; } = new();

        /// <summary>OT asignadas a la familia en Oracle: sus equipos son los del programa.</summary>
        public List<OtAsignadaDto> Ordenes { get; set; } = new();
    }

    public class OtAsignadaDto
    {
        public string NoOt { get; set; } = default!;
        public string CodigoLegado { get; set; } = default!;
        public decimal CantActividad { get; set; }
        public decimal CantDias { get; set; }
        /// <summary>Semana (del calendario del reporte) de la última actividad planificada: cuándo se programa entregarlo.</summary>
        public int? SemanaFin { get; set; }
    }

    public class SemanaPlanDto
    {
        public int NoSemana { get; set; }
        public int Mes { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal Actividad { get; set; }
        public decimal Dias { get; set; }
    }
}
