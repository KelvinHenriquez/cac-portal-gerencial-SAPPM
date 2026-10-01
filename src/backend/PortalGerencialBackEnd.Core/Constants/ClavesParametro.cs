namespace PortalGerencialBackEnd.Core.Constants
{
    /// <summary>Claves de PARAMETRO_PGPM. Los valores iniciales se siembran en la migración.</summary>
    public static class ClavesParametro
    {
        /// <summary>Estados de orden (separados por coma) que dejan al equipo fuera de servicio (sección 6.1, pendiente de confirmar).</summary>
        public const string EstadosFueraServicio = "SAP.ESTADOS_FUERA_SERVICIO";

        /// <summary>Estados de orden (separados por coma) que cuentan como orden terminada para el avance.</summary>
        public const string EstadosCerrados = "SAP.ESTADOS_CERRADOS";

        /// <summary>Clases de orden que cuentan para Avance de Reparación (sección 4.2).</summary>
        public const string ClasesOrdenAvance = "SAP.CLASES_ORDEN_AVANCE";

        /// <summary>Hora local (HH:mm) de la corrida diaria del job.</summary>
        public const string HoraCorridaDiaria = "JOB.HORA_CORRIDA_DIARIA";

        /// <summary>Días hacia atrás que se recalcula la disponibilidad diaria en cada corrida.</summary>
        public const string DiasRecalculoDisponibilidad = "JOB.DIAS_RECALCULO_DISPONIBILIDAD";

        /// <summary>Horas sin una corrida exitosa a partir de las cuales se alerta (sección 11).</summary>
        public const string HorasAlertaFrescura = "MONITOREO.HORAS_ALERTA";

        /// <summary>Programa de reparación que se abre por defecto en Avance.</summary>
        public const string ProgramaVigente = "AVANCE.PROGRAMA_VIGENTE";

        /// <summary>
        /// Fórmula provisional del % real de avance mientras se confirma "Total Ejec. %" (sección 6.2):
        /// HH_ORDENES_CERRADAS = horas plan de las órdenes cerradas / horas plan de todas las órdenes;
        /// ORDENES_CERRADAS = órdenes cerradas / total de órdenes.
        /// </summary>
        public const string FormulaAvanceReal = "AVANCE.FORMULA_REAL";

        /// <summary>Fecha (yyyy-MM-dd) desde la que se extrae de SAP la primera vez que corre el job.</summary>
        public const string FechaCargaInicial = "SAP.FECHA_CARGA_INICIAL";
    }

    /// <summary>Fuentes (pasos) que se registran en LOG_EXTRACCION_PGPM.</summary>
    public static class FuentesLog
    {
        public const string SapEquipos = "SAP_EQUIPOS";
        public const string SapOrdenes = "SAP_ORDENES";
        public const string SapHistorial = "SAP_HISTORIAL";
        public const string SapHoras = "SAP_HORAS";
        public const string CalcSinClasificar = "CALC_SIN_CLASIFICAR";
        public const string CalcDisponibilidad = "CALC_DISPONIBILIDAD";
        public const string CalcAvance = "CALC_AVANCE";

        public static readonly string[] Todas =
            { SapEquipos, SapOrdenes, SapHistorial, SapHoras, CalcSinClasificar, CalcDisponibilidad, CalcAvance };
    }

    public static class EstadosLog
    {
        public const string EnCurso = "EN_CURSO";
        public const string Ok = "OK";
        public const string Error = "ERROR";
    }
}
