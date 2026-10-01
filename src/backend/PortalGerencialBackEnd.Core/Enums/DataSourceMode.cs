namespace PortalGerencialBackEnd.Core.Enums
{
    /// <summary>
    /// Sección 9 de la especificación. En este portal Oracle está presente en ambos modos (en local
    /// corre Oracle Free en Docker), así que DataSourceMode solo decide de dónde salen los datos
    /// operativos de SAP PM: Local = cliente simulado sin SAP; Production = RFC/BAPI real vía NCo.
    /// </summary>
    public enum DataSourceMode
    {
        Local = 1,
        Production = 2
    }
}
