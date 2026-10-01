namespace PortalGerencialBackEnd.Core.Entities.Sap
{
    /// <summary>Espejo del maestro de equipos de SAP (EQUI, Anexo A.5). Tabla STG_SAP_EQUIPO_PGPM.</summary>
    public class StgSapEquipo
    {
        public string EquipoSapId { get; set; } = default!;

        /// <summary>Código viejo del equipo que SAP ya trae cargado: llave de cruce con Oracle (sección 3).</summary>
        public string? CodigoLegado { get; set; }

        public string? Descripcion { get; set; }
        public string? PuestoTrabajo { get; set; }
        public DateTime ActualizadoEn { get; set; }

        public ICollection<StgSapOrden> Ordenes { get; set; } = new List<StgSapOrden>();
    }
}
