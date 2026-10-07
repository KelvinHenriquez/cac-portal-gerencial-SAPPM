namespace PortalGerencialBackEnd.Core.Constants
{
    /// <summary>Claves de PARAMETRO_PGPM. Los valores iniciales se siembran en la migración.</summary>
    public static class ClavesParametro
    {
        /// <summary>
        /// Clase de orden (AUART) → tipo de horas del reporte de disponibilidad: lista
        /// "ZC01=CORRECTIVA,ZC02=PREVENTIVO,ZC03=PLANIFICADA". Una clase que no esté cuenta como PLANIFICADA.
        /// </summary>
        public const string TipoPorClaseOrden = "SAP.TIPO_POR_CLASE";

        /// <summary>Estados de orden (separados por coma) que cierran la orden (fin de la indisponibilidad).</summary>
        public const string EstadosCerrados = "SAP.ESTADOS_CERRADOS";

        /// <summary>Estados de orden que no cuentan para nada (p. ej. Anulada).</summary>
        public const string EstadosExcluidos = "SAP.ESTADOS_EXCLUIDOS";

        /// <summary>Fecha (yyyy-MM-dd) desde la que se extrae de SAP en la primera corrida.</summary>
        public const string FechaCargaInicial = "SAP.FECHA_CARGA_INICIAL";

        /// <summary>Clases de orden que dejan equipos fuera de servicio ("*" = todas).</summary>
        public const string ClasesOrdenDisponibilidad = "DISPONIBILIDAD.CLASES_ORDEN";

        /// <summary>Desde cuándo cuenta la indisponibilidad: CREACION (de la orden) o INICIO_REAL.</summary>
        public const string InicioVentana = "DISPONIBILIDAD.INICIO_VENTANA";

        /// <summary>
        /// S = una orden correctiva con aviso solo deja el equipo fuera si el aviso indica parada, y
        /// entonces cuenta desde el inicio hasta el fin de la avería del aviso.
        /// </summary>
        public const string CorrectivasSegunAviso = "DISPONIBILIDAD.CORRECTIVAS_SEGUN_AVISO";

        /// <summary>
        /// Usuarios SAP (separados por coma) que crean las operaciones de la hoja de ruta: una
        /// operación creada por uno de ellos es del plan original (Real Programado); por otro usuario,
        /// agregada en la reparación (Real No Programado). "-" = sin definir: se usa la marca que traiga SAP.
        /// </summary>
        public const string UsuariosHojaRuta = "SAP.USUARIOS_HOJA_RUTA";

        /// <summary>
        /// Base del % de disponibilidad: REQUERIDOS (disponibles / requeridos, topado en 100; decisión
        /// del 30/09/2026) o EQUIPOS (disponibles / equipos de la asignación, como PR_DISPONIBILIDAD_EQUIPOS
        /// de Oracle).
        /// </summary>
        public const string BasePorcentajeDisponibilidad = "DISPONIBILIDAD.BASE_PORCENTAJE";

        /// <summary>
        /// Fecha con la que una orden del programa cuenta como terminada en el avance "por órdenes":
        /// FIN_REAL (fin real de la orden, cuando terminó el trabajo) o CIERRE_TECNICO (cuando se hizo
        /// el cierre técnico en SAP, que puede ser días después y en lote).
        /// </summary>
        public const string FechaOrdenTerminadaAvance = "AVANCE.FECHA_ORDEN_TERMINADA";

        /// <summary>Clases de orden correctivas del reporte de permanencia (Centro de Servicios e Industrial).</summary>
        public const string ClasesOrdenPermanencia = "PERMANENCIA.CLASES_ORDEN";

        /// <summary>Áreas del reporte de permanencia por grupo de planificación del equipo: "CDS=Centro de Servicios,IND=Industrial".</summary>
        public const string AreasPermanencia = "PERMANENCIA.AREAS";

        /// <summary>Estados de usuario de la orden: "ENPR=E0002,FINL=E0003" (En proceso, Finalizada).</summary>
        public const string EstadosUsuarioPermanencia = "PERMANENCIA.ESTADOS_USUARIO";

        /// <summary>Estado de usuario "en espera de repuestos" (p. ej. E0004); "-" = todavía no existe en SAP.</summary>
        public const string EstadoEsperaRepuestos = "PERMANENCIA.ESTADO_ESPERA";

        /// <summary>
        /// Peso de cada actividad en el avance real "por actividades": CANTIDAD (cada una vale 1, como
        /// el PL/SQL de Oracle), HORAS_PLAN (trabajo planificado) o DIAS (duración entre sus fechas plan).
        /// En SAP las suboperaciones de una orden de reparación traen todas la misma fecha plan, así
        /// que DIAS daría 0 (07/10/2026).
        /// </summary>
        public const string PesoActividadAvance = "AVANCE.PESO_ACTIVIDAD";

        /// <summary>Clases de orden que cuentan para Avance de Reparación.</summary>
        public const string ClasesOrdenAvance = "AVANCE.CLASES_ORDEN";

        /// <summary>Con qué se arma el % programado de cada semana: ACTIVIDADES o DIAS del plan de Oracle.</summary>
        public const string BasePlanAvance = "AVANCE.BASE_PLAN";

        /// <summary>Hora local (HH:mm) de la corrida diaria del job.</summary>
        public const string HoraCorridaDiaria = "JOB.HORA_CORRIDA_DIARIA";

        /// <summary>Días hacia atrás que se recalcula la disponibilidad en cada corrida.</summary>
        public const string DiasRecalculoDisponibilidad = "JOB.DIAS_RECALCULO_DISPONIBILIDAD";

        /// <summary>Horas sin una corrida exitosa a partir de las cuales se alerta (sección 11).</summary>
        public const string HorasAlertaFrescura = "MONITOREO.HORAS_ALERTA";
    }

    /// <summary>Fuentes (pasos) que se registran en LOG_EXTRACCION_PGPM.</summary>
    public static class FuentesLog
    {
        public const string SapEquipos = "SAP_EQUIPOS";
        public const string SapOrdenes = "SAP_ORDENES";
        public const string SapOperaciones = "SAP_OPERACIONES";
        public const string SapAvisos = "SAP_AVISOS";
        public const string SapHistorial = "SAP_HISTORIAL";
        public const string CalcSinClasificar = "CALC_SIN_CLASIFICAR";
        public const string CalcDisponibilidad = "CALC_DISPONIBILIDAD";
        public const string CalcPermanencia = "CALC_PERMANENCIA";
        public const string CalcAvance = "CALC_AVANCE";
    }

    public static class EstadosLog
    {
        public const string EnCurso = "EN_CURSO";
        public const string Ok = "OK";
        public const string Error = "ERROR";
    }

    /// <summary>Tipos de horas OT del reporte de disponibilidad.</summary>
    public static class TiposOrden
    {
        public const string Planificada = "PLANIFICADA";
        public const string Preventivo = "PREVENTIVO";
        public const string Correctiva = "CORRECTIVA";
    }
}
