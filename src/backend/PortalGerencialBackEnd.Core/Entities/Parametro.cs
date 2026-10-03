namespace PortalGerencialBackEnd.Core.Entities
{
    /// <summary>
    /// Parámetros del portal (patrón SystemParameter de TPresto): reglas de negocio que hoy son
    /// supuestos pendientes de confirmar y que deben poder ajustarse sin desplegar código.
    /// Tabla PARAMETRO_PGPM.
    /// </summary>
    public class Parametro
    {
        public string Clave { get; set; } = default!;
        public string Valor { get; set; } = default!;
        public string? Descripcion { get; set; }
        public DateTime ActualizadoEn { get; set; }

        /// <summary>Usuario de Oracle que hizo el último cambio desde el portal (null = valor sembrado).</summary>
        public string? ActualizadoPor { get; set; }
    }
}
