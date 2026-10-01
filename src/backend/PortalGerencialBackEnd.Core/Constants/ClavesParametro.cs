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
    }
}
