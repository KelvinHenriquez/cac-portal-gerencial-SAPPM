namespace PortalGerencialBackEnd.Core.Interfaces.Integraciones
{
    public enum ResultadoValidacionOracle
    {
        Valido = 1,
        CredencialesInvalidas = 2,
        CuentaBloqueada = 3,
        PasswordVencido = 4,
        OracleNoDisponible = 5,
        /// <summary>Usuario y clave correctos, pero sin ningún rol del portal (ROL_PORTAL_*).</summary>
        SinRolDeAcceso = 6
    }

    /// <summary>
    /// Sección 4.3: el login abre una conexión de prueba contra Oracle con las credenciales exactas
    /// del usuario. Si conecta, el usuario existe, está activo y la clave es correcta — el portal no
    /// guarda ni administra usuarios.
    /// </summary>
    public interface IOracleCredentialValidator
    {
        /// <returns>Además del resultado, los roles del portal (ROL_PORTAL_*) que tiene el usuario.</returns>
        Task<(ResultadoValidacionOracle Resultado, bool PasswordPorVencer, IReadOnlyList<string> Roles)> ValidarAsync(string usuario, string password);
    }
}
