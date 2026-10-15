namespace PortalGerencialBackEnd.Core.Entities.Sap
{
    // Staging de SAP PM (Anexo A v2, docs/ANEXO_A_V2_ABAP.md). Espejo de lo que devuelven los
    // módulos RFC, solo con lo que usan los reportes. Las fechas guardan la hora (DATE de Oracle).

    /// <summary>Z_PM_TALLERES_EQUIPOS. Tabla STG_SAP_EQUIPO_PGPM.</summary>
    public class StgSapEquipo
    {
        public string EquipoSapId { get; set; } = default!;

        /// <summary>Código viejo (Ficha) que SAP ya trae cargado: llave de cruce con Oracle.</summary>
        public string? CodigoLegado { get; set; }

        public string? Descripcion { get; set; }
        public string? PuestoTrabajo { get; set; }

        /// <summary>Status INAC o marca de borrado en SAP: no cuenta en la disponibilidad.</summary>
        public bool Inactivo { get; set; }

        /// <summary>Tipo de equipo (EQART, p. ej. PM_TRACT "Tractores"): la "familia" del reporte de permanencia.</summary>
        public string? TipoEquipo { get; set; }
        public string? TipoEquipoDesc { get; set; }

        /// <summary>Grupo de planificación (INGRP): CDS = Centro de Servicio, IND = Industrial.</summary>
        public string? GrupoPlanificacion { get; set; }
        public string? GrupoPlanificacionDesc { get; set; }

        /// <summary>Ubicación técnica (TPLNR): para el futuro reporte por ubicación del área industrial.</summary>
        public string? UbicacionTecnica { get; set; }
        public string? UbicacionTecnicaDesc { get; set; }

        public DateTime ActualizadoEn { get; set; }

        public ICollection<StgSapOrden> Ordenes { get; set; } = new List<StgSapOrden>();
    }

    /// <summary>Z_PM_TALLERES_ORDENES. Tabla STG_SAP_ORDEN_PGPM.</summary>
    public class StgSapOrden
    {
        public string OrdenSapId { get; set; } = default!;

        /// <summary>AUART: ZC01, ZC02, ZC03...</summary>
        public string ClaseOrden { get; set; } = default!;

        /// <summary>ILART: clase de actividad PM (planificada / preventiva / correctiva).</summary>
        public string? ClaseActividad { get; set; }

        public string? EquipoSapId { get; set; }
        public string? Descripcion { get; set; }
        public string? PuestoTrabajo { get; set; }

        /// <summary>QMNUM: aviso que originó la orden.</summary>
        public string? AvisoSapId { get; set; }

        /// <summary>Estado normalizado: Creada, Liberada, En Proceso, Cerrada, Anulada.</summary>
        public string EstadoOrden { get; set; } = default!;

        public string? StatusSistema { get; set; }
        public string? StatusUsuario { get; set; }

        /// <summary>ERDAT + ERFZEIT: desde aquí el equipo cuenta fuera de servicio (respuesta del 01/10/2026).</summary>
        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaInicioPlan { get; set; }
        public DateTime? FechaFinPlan { get; set; }
        public DateTime? FechaInicioReal { get; set; }
        public DateTime? FechaFinReal { get; set; }

        /// <summary>Cierre técnico (primer I0045 vigente en JCDS): hasta aquí cuenta fuera de servicio.</summary>
        public DateTime? FechaCierre { get; set; }

        public DateTime ActualizadoEn { get; set; }

        public StgSapEquipo? Equipo { get; set; }
        public ICollection<StgSapOperacion> Operaciones { get; set; } = new List<StgSapOperacion>();
    }

    /// <summary>
    /// Z_PM_TALLERES_OPERACIONES: cada operación (actividad) de una orden. Terminada cuando tiene
    /// horas reales; cuenta en la semana en que se confirmaron. Tabla STG_SAP_OPERACION_PGPM.
    /// </summary>
    public class StgSapOperacion
    {
        public string OrdenSapId { get; set; } = default!;
        public string Operacion { get; set; } = default!;
        public string? Texto { get; set; }
        public string? PuestoTrabajo { get; set; }
        public string? Sistema { get; set; }
        public string? Subsistema { get; set; }
        public string? Componente { get; set; }
        public decimal TrabajoPlanHh { get; set; }
        public decimal TrabajoRealHh { get; set; }

        /// <summary>Fechas plan (FSAVD/FSEDD): de ellas salen los "días" de la actividad.</summary>
        public DateTime? FechaInicioPlan { get; set; }
        public DateTime? FechaFinPlan { get; set; }

        public DateTime? FechaPrimeraConf { get; set; }

        /// <summary>Fecha en que se confirmaron (digitaron) las horas reales: semana en que cuenta.</summary>
        public DateTime? FechaUltimaConf { get; set; }

        public bool ConfFinal { get; set; }

        /// <summary>
        /// False = no notificable (sin clave de control, STEUS vacío en SAP): no cuenta en el avance.
        /// Null = SAP no informó la clave de control (el servicio todavía no la trae): cuenta.
        /// </summary>
        public bool? Notificable { get; set; }

        /// <summary>True = del plan original de la orden; false = agregada en la reparación (Real No Programado).</summary>
        public bool Programada { get; set; } = true;

        /// <summary>Usuario SAP que creó la operación (ERNAM): distingue hoja de ruta de agregadas.</summary>
        public string? UsuarioCreacion { get; set; }

        public DateTime ActualizadoEn { get; set; }

        public StgSapOrden? Orden { get; set; }
    }

    /// <summary>
    /// Z_PM_TALLERES_AVISOS: el aviso dice si la avería deja el equipo parado (MSAUS) y desde/hasta
    /// cuándo. Tabla STG_SAP_AVISO_PGPM.
    /// </summary>
    public class StgSapAviso
    {
        public string AvisoSapId { get; set; } = default!;
        public string? ClaseAviso { get; set; }
        public string? Texto { get; set; }
        public string? EquipoSapId { get; set; }
        public string? OrdenSapId { get; set; }
        public DateTime FechaAviso { get; set; }
        public bool Parada { get; set; }
        public DateTime? InicioAveria { get; set; }
        public DateTime? FinAveria { get; set; }
        public string? StatusSistema { get; set; }
        public DateTime? FechaCierre { get; set; }
        public DateTime ActualizadoEn { get; set; }
    }

    /// <summary>Z_PM_TALLERES_HISTORIAL_ESTADO (JCDS). Tabla STG_SAP_HIST_ESTADO_PGPM.</summary>
    public class StgSapHistorialEstado
    {
        public string OrdenSapId { get; set; } = default!;
        public string EstadoCodigo { get; set; } = default!;
        public string NumeroCambio { get; set; } = default!;
        public string? EstadoTexto { get; set; }
        public DateTime FechaHoraCambio { get; set; }

        /// <summary>'I' = el estado se activó; 'D' = se desactivó.</summary>
        public string Indicador { get; set; } = default!;

        /// <summary>El cambio dejó el estado INACTIVO (JCDS-INACT): sirve para medir cuánto estuvo en él.</summary>
        public bool Inactivo { get; set; }

        public DateTime ActualizadoEn { get; set; }
    }
}
