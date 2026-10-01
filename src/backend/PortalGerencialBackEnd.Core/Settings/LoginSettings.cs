namespace PortalGerencialBackEnd.Core.Settings
{
    /// <summary>
    /// Límite de intentos fallidos del portal. Va por debajo del FAILED_LOGIN_ATTEMPTS de Oracle
    /// (10 en el perfil DEFAULT) para que alguien que se equivoca en el portal quede frenado aquí
    /// antes de que Oracle le bloquee la cuenta, que también usa en Oracle Forms.
    /// </summary>
    public class LoginSettings
    {
        public int MaxIntentosFallidos { get; set; } = 5;
        public int VentanaMinutos { get; set; } = 15;
        public int BloqueoMinutos { get; set; } = 15;
    }
}
