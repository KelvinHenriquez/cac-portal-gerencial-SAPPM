namespace PortalGerencialBackEnd.Core.Dtos.Sistema
{
    /// <summary>Encabezado "Última actualización" (sección 8.4) y estado de frescura (sección 11).</summary>
    public class EstadoActualizacionDto
    {
        /// <summary>Fin de la última corrida completa y exitosa; null si nunca hubo una.</summary>
        public DateTime? UltimaActualizacion { get; set; }

        public double? HorasDesdeUltimaActualizacion { get; set; }

        /// <summary>Más de MONITOREO.HORAS_ALERTA horas sin una corrida exitosa (o ninguna nunca).</summary>
        public bool Desactualizado { get; set; }

        public int HorasAlerta { get; set; }
        public bool CorridaEnCurso { get; set; }

        /// <summary>Si la corrida más reciente falló: cuándo y por qué (los datos mostrados son de la anterior OK).</summary>
        public DateTime? UltimoErrorFecha { get; set; }
        public string? UltimoErrorMensaje { get; set; }
    }

    public class PasoCorridaDto
    {
        public string Fuente { get; set; } = default!;
        public string Estado { get; set; } = default!;
        public DateTime Inicio { get; set; }
        public DateTime? Fin { get; set; }
        public int? RegistrosProcesados { get; set; }
        public DateTime? FechaCorteDesde { get; set; }
        public string? ErrorMensaje { get; set; }
    }

    public class CorridaDto
    {
        public string CorridaId { get; set; } = default!;
        public string? Origen { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime? Fin { get; set; }

        /// <summary>OK si todos los pasos terminaron OK; ERROR si alguno falló; EN_CURSO si no terminó.</summary>
        public string Estado { get; set; } = default!;

        public List<PasoCorridaDto> Pasos { get; set; } = new();
    }

    public class EquipoSinClasificarDto
    {
        public string EquipoSapId { get; set; } = default!;
        public string? CodigoLegado { get; set; }
        public string? Descripcion { get; set; }
        public string? PuestoTrabajo { get; set; }
        public string Motivo { get; set; } = default!;
        public int CantOrdenes { get; set; }
        public DateTime DetectadoEn { get; set; }
    }
}
