namespace PortalGerencialBackEnd.Core.Calculo
{
    /// <summary>
    /// Color del semáforo según los umbrales del tipo de reporte (VW_UMBRALES_SEMAFORO_PGPM):
    /// por debajo del umbral rojo = ROJO; por debajo del naranja = NARANJA; si no, VERDE.
    /// Se calcula al leer, así un cambio de umbral en Oracle se ve sin esperar al job.
    /// </summary>
    public static class Semaforo
    {
        public const string Rojo = "ROJO";
        public const string Naranja = "NARANJA";
        public const string Verde = "VERDE";

        public static string Color(decimal porcentaje, decimal umbralRojo, decimal umbralNaranja) =>
            porcentaje < umbralRojo ? Rojo
            : porcentaje < umbralNaranja ? Naranja
            : Verde;
    }
}
