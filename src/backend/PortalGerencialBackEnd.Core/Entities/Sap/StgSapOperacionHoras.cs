namespace PortalGerencialBackEnd.Core.Entities.Sap
{
    /// <summary>
    /// Horas planificadas (AFVC-ARBEI) vs. confirmadas (AFRU-ISMNW) por operación de una orden
    /// (Anexo A.4). Insumo del % de avance real. Tabla STG_SAP_OPERACION_HORAS_PGPM.
    /// </summary>
    public class StgSapOperacionHoras
    {
        public long Id { get; set; }
        public string OrdenSapId { get; set; } = default!;
        public string Operacion { get; set; } = default!;
        public decimal TrabajoPlanHh { get; set; }
        public decimal TrabajoRealHh { get; set; }
        public DateTime ActualizadoEn { get; set; }

        public StgSapOrden? Orden { get; set; }
    }
}
