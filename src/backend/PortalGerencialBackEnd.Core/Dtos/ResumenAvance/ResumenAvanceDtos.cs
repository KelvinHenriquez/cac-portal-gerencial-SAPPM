namespace PortalGerencialBackEnd.Core.Dtos.ResumenAvance
{
    /// <summary>"Avance Reparación Semana N": una semana, por grupo y familia, como la lámina del Taller.</summary>
    public class ResumenAvanceDto
    {
        public string Area { get; set; } = default!;
        /// <summary>ORDENES o ACTIVIDADES, como en el reporte de Avance.</summary>
        public string Medida { get; set; } = default!;
        public int NoSemana { get; set; }
        public DateTime SemanaInicio { get; set; }
        public DateTime SemanaFin { get; set; }
        public DateTime? FechaCorte { get; set; }
        public string? Periodo { get; set; }

        /// <summary>Puede configurar las filas y escribir notas (rol del área o TI).</summary>
        public bool PuedeEditar { get; set; }

        /// <summary>False = se muestra la configuración sugerida (todavía nadie la guardó).</summary>
        public bool ConfiguracionGuardada { get; set; }

        public List<SemanaResumenDto> Semanas { get; set; } = new();
        public List<GrupoResumenDto> Grupos { get; set; } = new();
    }

    public class SemanaResumenDto
    {
        public int NoSemana { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        /// <summary>PASADA / ACTUAL / FUTURA.</summary>
        public string Momento { get; set; } = default!;
    }

    public class GrupoResumenDto
    {
        public string Nombre { get; set; } = default!;
        public List<FilaResumenDto> Filas { get; set; } = new();
        /// <summary>Fila "General": cantidades sumadas y porcentajes ponderados por cantidad de máquinas.</summary>
        public FilaResumenDto General { get; set; } = default!;
    }

    public class FilaResumenDto
    {
        public int ReporteId { get; set; }
        public string FamiliaCodigo { get; set; } = default!;
        public string Nombre { get; set; } = default!;
        public int Cantidad { get; set; }
        /// <summary>% programado de la semana; null si la familia no tiene plan esa semana.</summary>
        public decimal? ProgSemanaPct { get; set; }
        public decimal? EjecSemanaPct { get; set; }
        public decimal ProgAcumPct { get; set; }
        public decimal EjecAcumPct { get; set; }
        public decimal DiferenciaPct { get; set; }
        /// <summary>Equipos cuya última actividad planificada (OT de Oracle) cae hasta esta semana.</summary>
        public int ProgramadosEntregar { get; set; }
        /// <summary>Equipos con todas sus órdenes de reparación de SAP del programa terminadas hasta el fin de la semana.</summary>
        public int Entregados { get; set; }
        public string? Nota { get; set; }
        public string? NotaPor { get; set; }
        public DateTime? NotaEn { get; set; }
    }

    // ------------------------------------------------------------ configuración

    public class ConfigResumenDto
    {
        public bool Guardada { get; set; }
        public List<FilaConfigResumenDto> Filas { get; set; } = new();
        /// <summary>Familias de los programas del período, para elegir.</summary>
        public List<OpcionResumenDto> Opciones { get; set; } = new();
    }

    public class FilaConfigResumenDto
    {
        public string Grupo { get; set; } = default!;
        public string Nombre { get; set; } = default!;
        public int ReporteId { get; set; }
        public string FamiliaCodigo { get; set; } = default!;
        public int Orden { get; set; }
    }

    public class OpcionResumenDto
    {
        public int ReporteId { get; set; }
        public string ReporteNombre { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public string FamiliaNombre { get; set; } = default!;
        public int CantEquipo { get; set; }
    }

    public class GuardarConfigResumenRequest
    {
        public List<FilaConfigResumenDto> Filas { get; set; } = new();
    }

    public class GuardarNotaResumenRequest
    {
        public int ReporteId { get; set; }
        public string FamiliaCodigo { get; set; } = default!;
        public DateTime SemanaFin { get; set; }
        /// <summary>Vacío = borrar la nota.</summary>
        public string? Texto { get; set; }
    }
}
