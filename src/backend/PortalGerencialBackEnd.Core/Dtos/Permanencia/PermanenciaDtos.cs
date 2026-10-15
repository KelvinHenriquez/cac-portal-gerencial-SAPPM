namespace PortalGerencialBackEnd.Core.Dtos.Permanencia
{
    // Reporte "Tiempo de Permanencia": órdenes correctivas, del aviso a la orden finalizada.
    // Todas las duraciones van en HORAS de calendario; la pantalla las muestra en días.

    public class AreaPermanenciaDto
    {
        public string Codigo { get; set; } = default!;
        public string Nombre { get; set; } = default!;
    }

    /// <summary>Promedio de horas de cada paso, sobre las órdenes terminadas que pasaron por él.</summary>
    public class PasosPermanenciaDto
    {
        public decimal? Respuesta { get; set; }
        public decimal? Liberacion { get; set; }
        public decimal? EsperaInicio { get; set; }
        public decimal? Ejecucion { get; set; }
        public decimal? CierreAdministrativo { get; set; }
        public decimal? EsperaRepuestos { get; set; }
    }

    /// <summary>Una fila agregada: el área, una familia (tipo de equipo) o un equipo.</summary>
    public class IndicadorPermanenciaDto
    {
        public string Codigo { get; set; } = default!;
        public string Nombre { get; set; } = default!;

        /// <summary>Solo en filas de equipo: Ficha o código SAP.</summary>
        public string? Clave { get; set; }

        public int Ordenes { get; set; }
        public int Equipos { get; set; }
        /// <summary>MTTR: tiempo medio de restauración (aviso → orden finalizada) de las terminadas en el período.</summary>
        public decimal PromedioHoras { get; set; }

        /// <summary>MTBF: tiempo medio entre fallas en el período; null si no hubo fallas.</summary>
        public decimal? MtbfHoras { get; set; }

        /// <summary>Correctivas que empezaron en el período (las fallas del MTBF).</summary>
        public int Fallas { get; set; }

        /// <summary>Equipos que cuenta el MTBF (incluye los que no fallaron).</summary>
        public int EquiposPoblacion { get; set; }
        public decimal MedianaHoras { get; set; }
        public decimal MaximoHoras { get; set; }
        public PasosPermanenciaDto Pasos { get; set; } = new();

        /// <summary>Órdenes todavía abiertas (no entran en los promedios) y su antigüedad promedio.</summary>
        public int OrdenesEnCurso { get; set; }
        public decimal? AntiguedadEnCursoHoras { get; set; }
    }

    public class ResumenPermanenciaDto
    {
        public string Area { get; set; } = default!;
        public string AreaNombre { get; set; } = default!;
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public DateTime? FechaCalculo { get; set; }
        public bool EsperaRepuestosConfigurada { get; set; }
        public IndicadorPermanenciaDto Total { get; set; } = default!;
        public List<IndicadorPermanenciaDto> Familias { get; set; } = new();
    }

    public class FamiliaPermanenciaDto
    {
        public IndicadorPermanenciaDto Familia { get; set; } = default!;
        public List<IndicadorPermanenciaDto> Equipos { get; set; } = new();
    }

    public class OrdenPermanenciaDto
    {
        public string OrdenSapId { get; set; } = default!;
        public string ClaseOrden { get; set; } = default!;
        public string? Descripcion { get; set; }
        public string? AvisoSapId { get; set; }
        /// <summary>Del aviso: true = se marcó que la máquina quedó parada; false = puede seguir trabajando; null = sin aviso.</summary>
        public bool? AvisoParada { get; set; }
        public DateTime? InicioAveria { get; set; }
        public DateTime? FinAveria { get; set; }
        public bool Terminada { get; set; }
        public DateTime Inicio { get; set; }
        public DateTime? FechaAviso { get; set; }
        public DateTime FechaCreada { get; set; }
        public DateTime? FechaLiberada { get; set; }
        public DateTime? FechaEnProceso { get; set; }
        public DateTime? FechaFinalizada { get; set; }
        public DateTime? FechaCierreTecnico { get; set; }
        public decimal HorasPermanencia { get; set; }
        public PasosPermanenciaDto Pasos { get; set; } = new();
    }

    public class EquipoPermanenciaDto
    {
        public IndicadorPermanenciaDto Equipo { get; set; } = default!;
        public string TipoEquipo { get; set; } = default!;
        public string? TipoEquipoDesc { get; set; }

        /// <summary>Órdenes del período y las abiertas, de la más reciente a la más vieja.</summary>
        public List<OrdenPermanenciaDto> Ordenes { get; set; } = new();
    }
}
