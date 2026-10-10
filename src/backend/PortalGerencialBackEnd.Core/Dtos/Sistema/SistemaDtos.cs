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

        /// <summary>LOCAL (SAP simulado) o PRODUCTION (RFC real).</summary>
        public string Modo { get; set; } = default!;

        public bool JobHabilitado { get; set; }

        /// <summary>Próxima corrida diaria programada (JOB.HORA_CORRIDA_DIARIA).</summary>
        public DateTime? ProximaCorrida { get; set; }
    }

    public class AlertaDto
    {
        public string Tipo { get; set; } = default!;
        public DateTime Fecha { get; set; }
        public string? Destinatarios { get; set; }
        public bool Enviada { get; set; }
        public string? Error { get; set; }
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

    // ------------------------------------------------------------ parámetros

    public class ParametroDto
    {
        public string Clave { get; set; } = default!;
        public string Seccion { get; set; } = default!;
        public string Nombre { get; set; } = default!;
        public string? Descripcion { get; set; }
        public string Valor { get; set; } = default!;

        /// <summary>Valores permitidos si es una lista cerrada; null = texto.</summary>
        public string[]? Opciones { get; set; }

        public DateTime ActualizadoEn { get; set; }
        public string? ActualizadoPor { get; set; }

        /// <summary>Técnico (SAP, horario): solo lo edita ROL_PORTAL_ADMIN.</summary>
        public bool Tecnico { get; set; }

        /// <summary>El usuario actual puede cambiar este parámetro.</summary>
        public bool Editable { get; set; }
    }

    public class ParametrosDto
    {
        /// <summary>El usuario puede cambiar al menos los parámetros de negocio (ROL_PORTAL_PARAMETROS o ADMIN).</summary>
        public bool PuedeEditar { get; set; }

        public List<ParametroDto> Parametros { get; set; } = new();
    }

    public class ActualizarParametroRequest
    {
        public string Valor { get; set; } = default!;
    }
}
