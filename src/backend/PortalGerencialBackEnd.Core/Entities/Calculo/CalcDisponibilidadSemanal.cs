namespace PortalGerencialBackEnd.Core.Entities.Calculo
{
    /// <summary>
    /// Agregado semanal de la disponibilidad diaria — lo que pintan las columnas semanales con
    /// semáforo (sección 8.2). No estaba en la spec: cierra el hueco entre lo que se calcula (por
    /// día) y lo que se muestra (por semana). Semana ISO, lunes a domingo.
    ///
    /// El semáforo NO se guarda aquí: se colorea al leer, contra los umbrales vigentes en Oracle,
    /// para que un cambio de umbral se vea sin esperar al job. Tabla CALC_DISP_SEMANAL_PGPM.
    /// </summary>
    public class CalcDisponibilidadSemanal
    {
        public string TipoReporte { get; set; } = default!;
        public string FamiliaCodigo { get; set; } = default!;
        public int AnioIso { get; set; }
        public int SemanaIso { get; set; }
        public DateTime FechaInicioSemana { get; set; }
        public DateTime FechaFinSemana { get; set; }

        /// <summary>Días con dato calculado en esa semana (menos de 7 en la semana en curso).</summary>
        public int DiasCalculados { get; set; }

        public int CantEquipo { get; set; }
        public int EqRequerido { get; set; }
        public decimal EqDisponibleProm { get; set; }
        public int EqDisponibleMin { get; set; }

        /// <summary>Promedio de los DispRealPct diarios — el valor que se muestra y colorea.</summary>
        public decimal DispRealPct { get; set; }

        /// <summary>Peor día de la semana, por si gerencia prefiere ver el mínimo en vez del promedio.</summary>
        public decimal DispRealMinPct { get; set; }

        public decimal DispPotencialPct { get; set; }

        public string CorridaId { get; set; } = default!;
        public DateTime GeneradoEn { get; set; }
    }
}
