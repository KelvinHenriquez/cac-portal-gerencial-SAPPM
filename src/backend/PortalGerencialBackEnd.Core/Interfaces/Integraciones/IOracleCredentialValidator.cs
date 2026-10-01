namespace PortalGerencialBackEnd.Core.Interfaces.Integraciones
{
    public enum ResultadoValidacionOracle
    {
        Valido = 1,
        CredencialesInvalidas = 2,
        CuentaBloqueada = 3,
        PasswordVencido = 4,
        OracleNoDisponible = 5
    }

    /// <summary>
    /// Sección 4.3: el login abre una conexión de prueba contra Oracle con las credenciales exactas
    /// del usuario. Si conecta, el usuario existe, está activo y la clave es correcta — el portal no
    /// guarda ni administra usuarios.
    /// </summary>
    public interface IOracleCredentialValidator
    {
        Task<(ResultadoValidacionOracle Resultado, bool PasswordPorVencer)> ValidarAsync(string usuario, string password);
    }
}
