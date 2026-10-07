namespace PortalGerencialBackEnd.Core.Dtos.Sap
{
    // Lo que devuelven los módulos RFC del Anexo A v2, ya convertido a tipos .NET. El resto del
    // backend solo conoce estos DTOs: no sabe si vienen de SAP real o del simulador local.

    public class EquipoSapDto
    {
        public string EquipoSapId { get; set; } = default!;
        public string? CodigoLegado { get; set; }
        public string? Descripcion { get; set; }
        public string? PuestoTrabajo { get; set; }
        public bool Inactivo { get; set; }
        public string? TipoEquipo { get; set; }
        public string? TipoEquipoDesc { get; set; }
        public string? GrupoPlanificacion { get; set; }
        public string? GrupoPlanificacionDesc { get; set; }
        public string? UbicacionTecnica { get; set; }
        public string? UbicacionTecnicaDesc { get; set; }
    }

    public class OrdenSapDto
    {
        public string OrdenSapId { get; set; } = default!;
        public string ClaseOrden { get; set; } = default!;
        public string? ClaseActividad { get; set; }
        public string? EquipoSapId { get; set; }
        public string? Descripcion { get; set; }
        public string? PuestoTrabajo { get; set; }
        public string? AvisoSapId { get; set; }
        public string EstadoOrden { get; set; } = default!;
        public string? StatusSistema { get; set; }
        public string? StatusUsuario { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaInicioPlan { get; set; }
        public DateTime? FechaFinPlan { get; set; }
        public DateTime? FechaInicioReal { get; set; }
        public DateTime? FechaFinReal { get; set; }
        public DateTime? FechaCierre { get; set; }

        /// <summary>Último cambio por cualquier vía (AEDAT, JCDS o AFRU): filtra el delta.</summary>
        public DateTime FechaModificacion { get; set; }
    }

    public class OperacionSapDto
    {
        public string OrdenSapId { get; set; } = default!;
        public string Operacion { get; set; } = default!;
        public string? Texto { get; set; }
        public string? PuestoTrabajo { get; set; }
        public string? Sistema { get; set; }
        public string? Subsistema { get; set; }
        public string? Componente { get; set; }
        public decimal TrabajoPlanHh { get; set; }
        public decimal TrabajoRealHh { get; set; }
        public DateTime? FechaInicioPlan { get; set; }
        public DateTime? FechaFinPlan { get; set; }
        public DateTime? FechaPrimeraConf { get; set; }
        public DateTime? FechaUltimaConf { get; set; }
        public bool ConfFinal { get; set; }
        public bool Programada { get; set; } = true;
        public string? UsuarioCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
    }

    public class AvisoSapDto
    {
        public string AvisoSapId { get; set; } = default!;
        public string? ClaseAviso { get; set; }
        public string? Texto { get; set; }
        public string? EquipoSapId { get; set; }
        public string? OrdenSapId { get; set; }
        public DateTime FechaAviso { get; set; }
        public bool Parada { get; set; }
        public DateTime? InicioAveria { get; set; }
        public DateTime? FinAveria { get; set; }
        public string? StatusSistema { get; set; }
        public DateTime? FechaCierre { get; set; }
        public DateTime FechaModificacion { get; set; }
    }

    public class HistorialEstadoDto
    {
        public string OrdenSapId { get; set; } = default!;
        public string EstadoCodigo { get; set; } = default!;
        public string? EstadoTexto { get; set; }
        public string NumeroCambio { get; set; } = default!;
        public DateTime FechaHoraCambio { get; set; }
        public string Indicador { get; set; } = default!;
        public bool Inactivo { get; set; }
    }
}
