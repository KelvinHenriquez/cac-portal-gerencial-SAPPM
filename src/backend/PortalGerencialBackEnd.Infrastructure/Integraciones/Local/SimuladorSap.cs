using PortalGerencialBackEnd.Core.Dtos.Sap;

namespace PortalGerencialBackEnd.Infrastructure.Integraciones.Local
{
    /// <summary>
    /// Genera datos de SAP PM verosímiles a partir de la estructura de Oracle, para desarrollar
    /// sin SAP (DataSourceMode=Local). Es determinista: cada equipo usa su propia semilla y todos
    /// los números aleatorios se sacan antes de mirar la fecha de hoy, así que el mismo día
    /// siempre produce las mismas órdenes, y con los días las órdenes avanzan solas
    /// (pendiente → liberada → en proceso → cerrada), como en SAP.
    ///
    /// Qué simula:
    /// - Una orden ZC03 (reparación del programa) por equipo activo de cada familia, repartidas
    ///   en la ventana de la familia con más carga hacia el medio, con atrasos y sobre-duraciones.
    /// - Órdenes ZC01 (correctivas) por averías aleatorias de 1 a 6 días.
    /// - Brechas de disciplina reales (sección 6.3): ~5% de reparaciones terminadas que nunca se
    ///   cerraron técnicamente, y ~15% de órdenes sin horas confirmadas.
    /// - Tres equipos que solo existen en SAP (sin código legado o con uno que Oracle no tiene),
    ///   para que aparezcan en "equipos sin clasificar".
    /// </summary>
    public static class SimuladorSap
    {
        public const string Pendiente = "Pendiente";
        public const string Liberado = "Liberado";
        public const string EnProceso = "En Proceso";
        public const string CerradoTecnicamente = "Cerrado tecnicamente";

        public record EquipoOracle(string CodigoLegado, string Descripcion, string FamiliaCodigo, bool Activo);
        public record FamiliaPrograma(string FamiliaCodigo, DateTime FechaEstInicio, DateTime FechaEstFin);

        public sealed class Datos
        {
            public List<EquipoSapDto> Equipos { get; } = new();
            public List<OrdenSapDto> Ordenes { get; } = new();
            public List<(OperacionHorasDto Operacion, DateTime Modificacion)> Operaciones { get; } = new();
            public List<HistorialEstadoDto> Historial { get; } = new();
        }

        private const double ProbabilidadAveriaDiaria = 0.006;

        public static Datos Generar(IEnumerable<EquipoOracle> equiposOracle, IEnumerable<FamiliaPrograma> familias, DateTime hoy)
        {
            hoy = hoy.Date;
            var datos = new Datos();

            // Si una familia está en varios programas, cuenta el más reciente que ya empezó.
            var ventanas = familias
                .GroupBy(f => f.FamiliaCodigo)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.FechaEstInicio <= hoy).ThenByDescending(f => f.FechaEstInicio).First());

            var inicioAverias = ventanas.Count == 0
                ? hoy.AddDays(-120)
                : ventanas.Values.Min(f => f.FechaEstInicio).AddDays(-30);

            var indice = 0;
            foreach (var equipo in equiposOracle.OrderBy(e => e.CodigoLegado, StringComparer.Ordinal))
            {
                indice++;
                var equipoSapId = (10000000 + indice).ToString();
                var puesto = "TALLER-" + equipo.FamiliaCodigo[..Math.Min(8, equipo.FamiliaCodigo.Length)];

                datos.Equipos.Add(new EquipoSapDto
                {
                    EquipoSapId = equipoSapId,
                    CodigoLegado = equipo.CodigoLegado,
                    Descripcion = equipo.Descripcion,
                    PuestoTrabajo = puesto
                });

                if (!equipo.Activo)
                    continue;

                var rng = new Random(SemillaEstable(equipo.CodigoLegado));
                if (ventanas.TryGetValue(equipo.FamiliaCodigo, out var ventana))
                    GenerarReparacion(datos, rng, indice, equipoSapId, equipo, puesto, ventana, hoy);

                GenerarAverias(datos, rng, indice, equipoSapId, puesto, inicioAverias, hoy);
            }

            // Equipos que solo existen en SAP (sección 10: no deben perderse en silencio).
            var soloSap = new[]
            {
                (Id: "99000001", Legado: (string?)null, Desc: "Bomba de riego Pivote 3"),
                (Id: "99000002", Legado: (string?)"TR-900", Desc: "Tractor sin ficha en Oracle"),
                (Id: "99000003", Legado: (string?)"XX-001", Desc: "Planta eléctrica taller")
            };
            var extra = 9000;
            foreach (var e in soloSap)
            {
                extra++;
                datos.Equipos.Add(new EquipoSapDto { EquipoSapId = e.Id, CodigoLegado = e.Legado, Descripcion = e.Desc, PuestoTrabajo = "TALLER-GENERAL" });
                var rng = new Random(SemillaEstable(e.Id));
                GenerarAverias(datos, rng, extra, e.Id, "TALLER-GENERAL", inicioAverias, hoy, probabilidad: 0.02);
            }

            return datos;
        }

        private static void GenerarReparacion(
            Datos datos, Random rng, int indice, string equipoSapId, EquipoOracle equipo, string puesto,
            FamiliaPrograma ventana, DateTime hoy)
        {
            // Todas las tiradas primero (determinismo: no dependen de hoy).
            var u = (rng.NextDouble() + rng.NextDouble()) / 2;
            var duracionPlan = rng.Next(10, 36);
            var atrasoInicio = rng.Next(-2, 8);
            var factorDuracion = 0.8 + rng.NextDouble() * 0.6;
            var nuncaSeCerro = rng.NextDouble() < 0.05;
            var sinConfirmaciones = rng.NextDouble() < 0.15;
            var horaEvento = rng.Next(7, 16);
            var cantOperaciones = rng.Next(2, 5);
            var operaciones = Enumerable.Range(1, cantOperaciones)
                .Select(_ => (Plan: (decimal)rng.Next(8, 61), Factor: 0.6 + rng.NextDouble() * 0.7))
                .ToList();

            var largo = (ventana.FechaEstFin - ventana.FechaEstInicio).TotalDays;
            var inicioPlan = ventana.FechaEstInicio.AddDays(Math.Floor(u * largo * 0.75));
            var finPlan = inicioPlan.AddDays(duracionPlan);
            if (finPlan > ventana.FechaEstFin) finPlan = ventana.FechaEstFin;
            var inicioReal = inicioPlan.AddDays(atrasoInicio);
            var finReal = inicioReal.AddDays(Math.Ceiling(duracionPlan * factorDuracion));
            var creacion = inicioPlan.AddDays(-14);

            if (creacion > hoy)
                return; // todavía no la crearon en SAP

            var liberacion = inicioPlan.AddDays(-7) < creacion ? creacion : inicioPlan.AddDays(-7);
            string estado;
            DateTime? inicioRealOrden = null, finRealOrden = null;
            double progreso;

            if (inicioReal > hoy)
            {
                estado = liberacion <= hoy ? Liberado : Pendiente;
                progreso = 0;
            }
            else if (finReal > hoy)
            {
                estado = EnProceso;
                inicioRealOrden = inicioReal;
                progreso = (hoy - inicioReal).TotalDays / Math.Max(1, (finReal - inicioReal).TotalDays);
            }
            else if (nuncaSeCerro)
            {
                estado = EnProceso; // terminó en el taller, pero nadie la cerró técnicamente en SAP
                inicioRealOrden = inicioReal;
                progreso = 1;
            }
            else
            {
                estado = CerradoTecnicamente;
                inicioRealOrden = inicioReal;
                finRealOrden = finReal;
                progreso = 1;
            }

            var ordenId = $"4{indice:D4}001";
            var eventos = new List<(string Codigo, string Texto, DateTime Fecha, string Indicador)>
            {
                ("I0001", "CREA", creacion, "I")
            };
            if (liberacion <= hoy) eventos.Add(("I0002", "LIB.", liberacion, "I"));
            if (inicioRealOrden != null) eventos.Add(("E0002", "PROC", inicioReal, "I"));
            if (finRealOrden != null)
            {
                eventos.Add(("E0002", "PROC", finReal, "D"));
                eventos.Add(("I0045", "CTEC", finReal, "I"));
            }

            var modificacion = eventos.Max(e => e.Fecha);
            datos.Ordenes.Add(new OrdenSapDto
            {
                OrdenSapId = ordenId,
                ClaseOrden = "ZC03",
                EquipoSapId = equipoSapId,
                Descripcion = $"Reparación interzafra {equipo.CodigoLegado}",
                PuestoTrabajo = puesto,
                EstadoOrden = estado,
                FechaCreacion = creacion,
                FechaInicioPlan = inicioPlan,
                FechaFinPlan = finPlan,
                FechaInicioReal = inicioRealOrden,
                FechaFinReal = finRealOrden,
                FechaModificacion = modificacion
            });

            AgregarHistorial(datos, ordenId, eventos, horaEvento);

            for (var i = 0; i < operaciones.Count; i++)
            {
                var (plan, factor) = operaciones[i];
                var real = sinConfirmaciones ? 0m : Math.Round(plan * (decimal)(Math.Min(1, progreso) * factor), 2);
                datos.Operaciones.Add((new OperacionHorasDto
                {
                    OrdenSapId = ordenId,
                    Operacion = ((i + 1) * 10).ToString("D4"),
                    TrabajoPlanHh = plan,
                    TrabajoRealHh = real
                }, modificacion));
            }
        }

        private static void GenerarAverias(
            Datos datos, Random rng, int indice, string equipoSapId, string puesto, DateTime desde, DateTime hoy,
            double probabilidad = ProbabilidadAveriaDiaria)
        {
            var secuencia = 0;
            for (var dia = desde.Date; dia <= hoy; dia = dia.AddDays(1))
            {
                if (rng.NextDouble() >= probabilidad)
                    continue;

                var duracion = rng.Next(1, 7);
                var planHh = (decimal)rng.Next(4, 17);
                var factor = 0.7 + rng.NextDouble() * 0.6;
                var hora = rng.Next(6, 18);

                secuencia++;
                var ordenId = $"3{indice:D4}{secuencia:D3}";
                var fin = dia.AddDays(duracion);
                var cerrada = fin <= hoy;

                var eventos = new List<(string Codigo, string Texto, DateTime Fecha, string Indicador)>
                {
                    ("I0001", "CREA", dia, "I"),
                    ("I0002", "LIB.", dia, "I"),
                    ("E0002", "PROC", dia, "I")
                };
                if (cerrada)
                {
                    eventos.Add(("E0002", "PROC", fin, "D"));
                    eventos.Add(("I0045", "CTEC", fin, "I"));
                }

                var modificacion = eventos.Max(e => e.Fecha);
                datos.Ordenes.Add(new OrdenSapDto
                {
                    OrdenSapId = ordenId,
                    ClaseOrden = "ZC01",
                    EquipoSapId = equipoSapId,
                    Descripcion = "Correctivo por avería",
                    PuestoTrabajo = puesto,
                    EstadoOrden = cerrada ? CerradoTecnicamente : EnProceso,
                    FechaCreacion = dia,
                    FechaInicioPlan = dia,
                    FechaFinPlan = fin,
                    FechaInicioReal = dia,
                    FechaFinReal = cerrada ? fin : null,
                    FechaModificacion = modificacion
                });
                AgregarHistorial(datos, ordenId, eventos, hora);

                var progreso = cerrada ? 1 : (hoy - dia).TotalDays / duracion;
                datos.Operaciones.Add((new OperacionHorasDto
                {
                    OrdenSapId = ordenId,
                    Operacion = "0010",
                    TrabajoPlanHh = planHh,
                    TrabajoRealHh = Math.Round(planHh * (decimal)(progreso * factor), 2)
                }, modificacion));

                dia = fin; // no se vuelve a averiar mientras está en reparación
            }
        }

        private static void AgregarHistorial(
            Datos datos, string ordenId, List<(string Codigo, string Texto, DateTime Fecha, string Indicador)> eventos, int hora)
        {
            foreach (var grupo in eventos.GroupBy(e => e.Codigo))
            {
                var numero = 0;
                foreach (var e in grupo.OrderBy(e => e.Fecha))
                {
                    numero++;
                    datos.Historial.Add(new HistorialEstadoDto
                    {
                        OrdenSapId = ordenId,
                        EstadoCodigo = e.Codigo,
                        EstadoTexto = e.Texto,
                        NumeroCambio = numero.ToString("D3"),
                        FechaHoraCambio = e.Fecha.Date.AddHours(hora),
                        Indicador = e.Indicador
                    });
                }
            }
        }

        /// <summary>FNV-1a: string.GetHashCode cambia en cada proceso de .NET, esto no.</summary>
        private static int SemillaEstable(string texto)
        {
            unchecked
            {
                var hash = 2166136261;
                foreach (var c in texto)
                {
                    hash ^= c;
                    hash *= 16777619;
                }
                return (int)hash;
            }
        }
    }
}
