namespace PortalGerencialBackEnd.Core.Entities.Calculo
{
    /// <summary>
    /// Equipos de SAP que no se pudieron asignar a ninguna familia de Oracle (sección 10): en vez
    /// de perderse del cálculo en silencio, el job los deja aquí en cada corrida.
    /// Tabla CALC_EQUIPO_SIN_CLASIF_PGPM.
    /// </summary>
    public class CalcEquipoSinClasificar
    {
        public string EquipoSapId { get; set; } = default!;
        public string? CodigoLegado { get; set; }
        public string? Descripcion { get; set; }
        public string? PuestoTrabajo { get; set; }

        /// <summary>SIN_CODIGO_LEGADO (vacío en SAP) o CODIGO_NO_EXISTE_EN_ORACLE.</summary>
        public string Motivo { get; set; } = default!;

        /// <summary>Órdenes del equipo en staging — para priorizar cuáles corregir primero.</summary>
        public int CantOrdenes { get; set; }

        public string CorridaId { get; set; } = default!;
        public DateTime DetectadoEn { get; set; }
    }
}
