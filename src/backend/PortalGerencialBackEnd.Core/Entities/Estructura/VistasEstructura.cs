namespace PortalGerencialBackEnd.Core.Entities.Estructura
{
    // Filas de las vistas VW_*_PGPM: la configuración que vive en Oracle Forms (pantallas
    // MA_CONFIGURACION_DISP y MA_CONFIG_AVANCE) y el portal solo lee. Se mapean como entidades sin
    // llave sobre vistas, así que EF no las crea en las migraciones: las vistas las crea un script
    // por ambiente (db/oracle/init/03_vistas_pgpm.sql en local, docs/oracle/vistas_produccion.sql
    // en producción). Las banderas vienen como 'S'/'N'.

    public class VwEquipoFamilia
    {
        /// <summary>Ficha del equipo (F01168): el código con que hoy se cruza con SAP.</summary>
        public string CodigoLegado { get; set; } = default!;

        /// <summary>Código de equipo de Oracle (A29017-002).</summary>
        public string? EquipoCodigo { get; set; }

        public string Descripcion { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public string Activo { get; set; } = default!;
    }

    // ------------------------------------------------------------ disponibilidad

    /// <summary>Paso 1: tipo de reporte. Rojo si &lt; UmbralRojo; naranja si &lt; UmbralNaranja.</summary>
    public class VwDispReporte
    {
        public int ReporteId { get; set; }
        public int Orden { get; set; }
        public string Descripcion { get; set; } = default!;
        public decimal UmbralRojo { get; set; }
        public decimal UmbralNaranja { get; set; }
        public string Activo { get; set; } = default!;
    }

    /// <summary>Paso 2: semanas del calendario del reporte.</summary>
    public class VwDispSemana
    {
        public int ReporteId { get; set; }
        public int NoSemana { get; set; }
        public int Mes { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }

    /// <summary>Paso 3: horas del día en que se mide la disponibilidad.</summary>
    public class VwDispHora
    {
        public int ReporteId { get; set; }
        public int Hora { get; set; }
        public string Activo { get; set; } = default!;
    }

    /// <summary>Paso 4: asignaciones (filas del reporte).</summary>
    public class VwDispAsignacion
    {
        public int ReporteId { get; set; }
        public int AsignacionId { get; set; }
        public int Orden { get; set; }
        public string Asignacion { get; set; } = default!;
        public int CantAsignada { get; set; }
        public int CantRequerida { get; set; }
        public decimal DispCompPct { get; set; }
        public string Negrita { get; set; } = default!;
        public string Grupo { get; set; } = default!;
    }

    /// <summary>Equipos de cada asignación (grupos ya resueltos a equipos individuales).</summary>
    public class VwDispAsigEquipo
    {
        public int ReporteId { get; set; }
        public int AsignacionId { get; set; }
        public string CodigoLegado { get; set; } = default!;
    }

    // ------------------------------------------------------------ avance

    /// <summary>Período de reparación (p. ej. 30: 2025 - 2026).</summary>
    public class VwAvancePeriodo
    {
        public int PeriodoId { get; set; }
        public string Descripcion { get; set; } = default!;
    }

    /// <summary>Reporte de avance (programa) dentro de un período.</summary>
    public class VwAvanceReporte
    {
        public int ReporteId { get; set; }
        public int PeriodoId { get; set; }
        public string Nombre { get; set; } = default!;
        public string TipoOt { get; set; } = default!;
        public string FechaRef { get; set; } = default!;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
    }

    public class VwAvanceFamilia
    {
        public int ReporteId { get; set; }
        public int Orden { get; set; }
        public string FamiliaCodigo { get; set; } = default!;
        public string FamiliaNombre { get; set; } = default!;
        public DateTime FechaEstInicio { get; set; }
        public DateTime FechaEstFin { get; set; }
        public int CantEquipo { get; set; }
    }

    /// <summary>
    /// "Órdenes de Trabajo Asignada a Familia": las OT de Oracle (y por lo tanto los equipos) que
    /// forman el programa de esa familia en ese reporte.
    /// </summary>
    public class VwAvanceOt
    {
        public int ReporteId { get; set; }
        public string FamiliaCodigo { get; set; } = default!;
        public string NoOt { get; set; } = default!;
        public string CodigoLegado { get; set; } = default!;
        public decimal CantActividad { get; set; }
        public decimal CantDias { get; set; }
    }

    /// <summary>"Semana Definida Por Familia": actividades y días planificados de cada semana.</summary>
    public class VwAvanceSemana
    {
        public int ReporteId { get; set; }
        public string FamiliaCodigo { get; set; } = default!;
        public int NoSemana { get; set; }
        public int Mes { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public decimal Actividad { get; set; }
        public decimal Dias { get; set; }
    }
}
