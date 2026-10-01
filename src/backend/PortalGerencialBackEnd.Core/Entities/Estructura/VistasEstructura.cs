namespace PortalGerencialBackEnd.Core.Entities.Estructura
{
    // Filas de las vistas VW_*_PGPM (sección 4.1): estructura que vive en Oracle Forms y el
    // portal solo lee. Se mapean como entidades sin llave sobre vistas, así que EF no las crea
    // en las migraciones: las vistas las crea un script por ambiente
    // (db/oracle/init/03_vistas_pgpm.sql en local, docs/oracle/vistas_produccion.sql en prod).

    public class VwUmbralSemaforo
    {
        public string TipoReporte { get; set; } = default!;
        public string Descripcion { get; set; } = default!;
        public decimal UmbralRojo { get; set; }
        public decimal UmbralNaranja { get; set; }
    }

    public class VwEstructuraFamilia
    {
        public string AnioPrograma { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public string FamiliaNombre { get; set; } = default!;
        public DateTime FechaEstInicio { get; set; }
        public DateTime FechaEstFin { get; set; }
        public int CantEquipo { get; set; }
    }

    public class VwCurvaPlaneada
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

    public class VwEquipoRequerido
    {
        public string TipoReporte { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public string Asignacion { get; set; } = default!;
        public int CantAsignada { get; set; }
        public int CantRequerida { get; set; }
    }

    public class VwEquipoFamilia
    {
        public string CodigoLegado { get; set; } = default!;
        public string Descripcion { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public string Activo { get; set; } = default!;
    }
}
