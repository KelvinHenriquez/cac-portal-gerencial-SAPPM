namespace PortalGerencialBackEnd.Core.Calculo
{
    /// <summary>
    /// Cuándo avisar que los datos están desactualizados (sección 11): cuando lo están y todavía no
    /// se avisó por este problema, o ya pasó el intervalo de repetición desde el último aviso. Un
    /// aviso anterior a la última corrida exitosa pertenece a un problema ya resuelto.
    /// </summary>
    public static class ReglaAlertaFrescura
    {
        public static bool DebeAlertar(
            bool desactualizado,
            DateTime? ultimaActualizacionOk,
            DateTime? ultimaAlerta,
            DateTime ahora,
            TimeSpan repetirCada)
        {
            if (!desactualizado)
                return false;
            if (ultimaAlerta == null)
                return true;
            if (ultimaActualizacionOk != null && ultimaAlerta < ultimaActualizacionOk)
                return true; // el aviso anterior era de otro problema, ya resuelto
            return ahora - ultimaAlerta.Value >= repetirCada;
        }
    }
}
