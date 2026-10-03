using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PortalGerencialBackEnd.Core.Dtos.Sap;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;

namespace PortalGerencialBackEnd.Infrastructure.Integraciones.Excel
{
    /// <summary>
    /// DataSourceMode=Excel (Sprint 0): "SAP" sale de los exports que el usuario baja de SAP y deja
    /// en la carpeta SapExcel:Carpeta. Llena el mismo staging que el RFC, así que el cálculo es el de
    /// producción. Cada archivo se reconoce por sus columnas, no por el nombre, y vale tanto el
    /// layout de las transacciones (IW39, IH08, IW37N, IW47, IW29) como el resultado de los módulos
    /// del ABAP bajado desde SE37 (EQUNR, KEY_NUM, ...). Ver docs/SPRINT0_EXPORTS_SAP.md.
    ///
    /// Los archivos son una foto: se devuelve todo lo que traen, sin mirar la fecha de corte.
    /// </summary>
    public class SapExcelExtractionClient : ISapExtractionClient
    {
        /// <summary>Hora que se asume cuando el export no trae la hora (la fecha sí): mediodía.</summary>
        public static readonly TimeSpan HoraSupuesta = new(12, 0, 0);

        // ------------------------------------------------------------ nombres de columna
        private static readonly string[] Orden = { "AUFNR", "Orden", "Orden de mantenimiento", "Número de orden", "Nº orden" };
        private static readonly string[] ClaseOrden = { "AUART", "Clase de orden", "Cl.orden", "Clase orden", "Cl. orden" };
        private static readonly string[] ClaseActividad = { "ILART", "Clase de actividad PM", "Cl.actividad PM", "Clase actividad PM", "Cl. actividad PM" };
        private static readonly string[] Equipo = { "EQUNR", "Equipo", "Número de equipo", "Nº equipo" };
        private static readonly string[] TextoOrden = { "KTEXT", "Texto breve", "Descripción", "Texto breve de orden" };
        private static readonly string[] PuestoResponsable = { "VAPLZ", "Puesto de trabajo responsable", "Pto.tbjo.responsable", "Pto.trab.resp.", "Puesto trabajo responsable" };
        private static readonly string[] Aviso = { "QMNUM", "Aviso", "Número de aviso", "Nº aviso" };
        private static readonly string[] StatusSistema = { "STTXT", "STATUS_SISTEMA", "Status del sistema", "Status sistema", "Estado de sistema", "Status de sistema" };
        private static readonly string[] StatusUsuario = { "USTXT", "STATUS_USUARIO", "Status de usuario", "Status usuario", "Estado de usuario" };
        private static readonly string[] FechaCreacion = { "ERDAT", "Fecha de creación", "Creado el", "Fecha creación" };
        private static readonly string[] HoraCreacion = { "ERFZEIT", "Hora de creación", "Hora creación", "Creado a las" };
        private static readonly string[] InicioPlan = { "GSTRP", "Fecha de inicio extrema", "Inicio extremo", "Fecha inicio extrema", "Fecha de inicio básica", "Inicio básico", "Fecha inicio básica" };
        private static readonly string[] HoraInicioPlan = { "GSUZP", "Hora de inicio básica", "Hora inicio básica" };
        private static readonly string[] FinPlan = { "GLTRP", "Fecha de fin extrema", "Fin extremo", "Fecha fin extrema", "Fecha de fin básica", "Fin básico", "Fecha fin básica" };
        private static readonly string[] HoraFinPlan = { "GLUZP", "Hora de fin básica", "Hora fin básica" };
        private static readonly string[] InicioReal = { "GSTRI", "Fecha de inicio real", "Inicio real", "Fe.inicio real", "Fecha inicio real" };
        private static readonly string[] HoraInicioReal = { "GSUZI", "Hora de inicio real", "Hora inicio real" };
        private static readonly string[] FinReal = { "GLTRI", "Fecha de fin real", "Fin real", "Fe.fin real", "Fecha fin real" };
        private static readonly string[] HoraFinReal = { "GEUZI", "Hora de fin real", "Hora fin real" };
        private static readonly string[] CierreTecnico = { "FECHA_TECO", "IDAT2", "Fecha de cierre técnico", "Fe.cierre técnico", "Cierre técnico", "Fecha cierre técnico" };
        private static readonly string[] HoraCierreTecnico = { "HORA_TECO", "Hora de cierre técnico", "Hora cierre técnico" };
        private static readonly string[] Modificacion = { "AEDAT", "Fecha de modificación", "Modificado el", "Fecha modificación", "Fecha de última modificación" };

        private static readonly string[] DescripcionEquipo = { "EQKTX", "Denominación", "Denominación de objeto técnico", "Descripción del equipo", "Descripción" };
        private static readonly string[] CodigoLegado = { "KEY_NUM", "FLEET_NUM", "Código legado", "Número de flota", "Nº de flota", "Ficha", "Ficha del equipo Oracle" };
        private static readonly string[] PuestoEquipo = { "WORKCENTER", "ARBPL", "GEWRK", "Puesto de trabajo responsable", "Pto.tbjo.responsable", "Puesto de trabajo" };
        private static readonly string[] EstadoEquipo = { "ESTADO_EQUIPO", "STTXT", "Status del sistema", "Estado de equipo", "Status sistema" };

        private static readonly string[] Operacion = { "VORNR", "Operación", "Op.", "Número de operación", "Nº operación" };
        private static readonly string[] TextoOperacion = { "LTXA1", "Texto breve operación", "Txt.brv.operación", "Texto de operación", "Texto breve" };
        private static readonly string[] PuestoOperacion = { "ARBPL", "Puesto de trabajo", "Pto.tbjo.", "Pto.trabajo" };
        private static readonly string[] Trabajo = { "ARBEI", "Trabajo", "Trabajo planificado" };
        private static readonly string[] UnidadTrabajo = { "ARBEH", "Unidad de trabajo", "Unidad trabajo", "Un.trabajo" };
        private static readonly string[] TrabajoReal = { "ISMNW", "ISMNW_TOTAL", "Trabajo real" };
        private static readonly string[] UnidadTrabajoReal = { "ISMNE", "Unidad trabajo real", "Unidad de trabajo real" };
        private static readonly string[] InicioOperacion = { "FSAVD", "Fecha de inicio más temprana", "Inicio más temprano", "Fecha inicio más temprana", "Inicio programado" };
        private static readonly string[] HoraInicioOperacion = { "FSAVZ", "Hora de inicio más temprana", "Hora inicio más temprana" };
        private static readonly string[] FinOperacion = { "FSEDD", "Fecha de fin más temprana", "Fin más temprano", "Fecha fin más temprana", "Fin programado" };
        private static readonly string[] HoraFinOperacion = { "FSEDZ", "Hora de fin más temprana", "Hora fin más temprana" };
        private static readonly string[] FinRealOperacion = { "IEDD", "Fecha de fin real", "Fin real", "Fecha fin real" };
        private static readonly string[] PrimeraConf = { "FECHA_PRIMERA_CONF" };
        private static readonly string[] UltimaConf = { "FECHA_ULTIMA_CONF" };
        private static readonly string[] ConfFinal = { "CONF_FINAL" };
        private static readonly string[] CreadoPor = { "ERNAM", "Creado por", "Autor" };
        private static readonly string[] Sistema = { "SISTEMA" };
        private static readonly string[] Subsistema = { "SUBSISTEMA" };
        private static readonly string[] Componente = { "COMPONENTE" };

        private static readonly string[] Notificacion = { "RUECK", "Notificación", "Número de notificación", "Nº notificación" };
        private static readonly string[] FechaContabilizacion = { "BUDAT", "Fecha de contabilización", "Fecha contab.", "Fe.contabilización", "Fecha contabilización" };
        private static readonly string[] FechaCaptura = { "ERSDA", "Fecha de entrada", "Fecha de captura", "Fecha entrada", "Registrado el" };
        private static readonly string[] NotificacionFinal = { "AUERU", "Notificación final", "Notif.final", "Confirmación final", "Notif. final" };
        private static readonly string[] Anulada = { "STOKZ", "Anulado", "Indicador de anulación", "Anulación", "Anulada" };
        private static readonly string[] ContadorAnulacion = { "STZHL", "Contador de anulación", "Contador anulación" };

        private static readonly string[] ClaseAviso = { "QMART", "Clase de aviso", "Cl.aviso", "Clase aviso" };
        private static readonly string[] TextoAviso = { "QMTXT", "Texto breve", "Descripción", "Texto breve del aviso" };
        private static readonly string[] FechaAviso = { "QMDAT", "Fecha de aviso", "Fecha aviso", "Fecha del aviso" };
        private static readonly string[] HoraAviso = { "MZEIT", "Hora de aviso", "Hora del aviso", "Hora aviso" };
        private static readonly string[] Parada = { "MSAUS", "Parada", "Indicador de parada" };
        private static readonly string[] InicioAveria = { "AUSVN", "Inicio de avería", "Inicio avería", "Fecha de inicio de la avería", "Inicio de la avería" };
        private static readonly string[] HoraInicioAveria = { "AUZTV", "Hora de inicio de avería", "Hora inicio avería", "Hora de inicio de la avería" };
        private static readonly string[] FinAveria = { "AUSBS", "Fin de avería", "Fin avería", "Fecha de fin de la avería", "Fin de la avería" };
        private static readonly string[] HoraFinAveria = { "AUZTB", "Hora de fin de avería", "Hora fin avería", "Hora de fin de la avería" };
        private static readonly string[] CierreAviso = { "QMDAB", "Fecha de cierre", "Fecha cierre" };

        private static readonly string[] Objeto = { "OBJNR", "Número de objeto" };
        private static readonly string[] Estado = { "STAT", "Status", "Estado" };
        private static readonly string[] NumeroCambio = { "CHGNR", "Número de modificación" };
        private static readonly string[] FechaCambio = { "UDATE", "Fecha de modificación", "Fecha" };
        private static readonly string[] HoraCambio = { "UTIME", "Hora de modificación", "Hora" };
        private static readonly string[] IndicadorCambio = { "CHIND", "Indicador de modificación" };

        private static readonly HashSet<string> Conocidos = typeof(SapExcelExtractionClient)
            .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string[]))
            .SelectMany(f => (string[])f.GetValue(null)!)
            .Select(LectorTablaExcel.Normalizar)
            .ToHashSet();

        private readonly string _carpeta;
        private readonly ILogger<SapExcelExtractionClient> _logger;
        private readonly Lazy<Datos> _datos;

        public SapExcelExtractionClient(IConfiguration configuration, ILogger<SapExcelExtractionClient> logger)
        {
            _carpeta = configuration["SapExcel:Carpeta"] ?? "/sprint0/sap";
            _logger = logger;
            _datos = new Lazy<Datos>(Cargar);
        }

        public Task<List<EquipoSapDto>> GetEquiposAsync(CancellationToken ct = default) => Task.FromResult(_datos.Value.Equipos);

        public Task<List<OrdenSapDto>> GetOrdenesModificadasDesdeAsync(DateTime desde, CancellationToken ct = default) => Task.FromResult(_datos.Value.Ordenes);

        public Task<List<OperacionSapDto>> GetOperacionesModificadasDesdeAsync(DateTime desde, CancellationToken ct = default) => Task.FromResult(_datos.Value.Operaciones);

        public Task<List<AvisoSapDto>> GetAvisosModificadosDesdeAsync(DateTime desde, CancellationToken ct = default) => Task.FromResult(_datos.Value.Avisos);

        public Task<List<HistorialEstadoDto>> GetHistorialEstadosDesdeAsync(DateTime desde, CancellationToken ct = default) => Task.FromResult(_datos.Value.Historial);

        // ------------------------------------------------------------ carga

        internal sealed class Datos
        {
            public List<EquipoSapDto> Equipos { get; } = new();
            public List<OrdenSapDto> Ordenes { get; } = new();
            public List<OperacionSapDto> Operaciones { get; } = new();
            public List<AvisoSapDto> Avisos { get; } = new();
            public List<HistorialEstadoDto> Historial { get; } = new();
            public List<string> Mensajes { get; } = new();   // mensajes para la bitácora
        }

        private Datos Cargar()
        {
            var datos = LeerCarpeta(_carpeta, DateTime.Now);
            foreach (var m in datos.Mensajes)
                _logger.LogWarning("Importador SAP Excel: {Mensaje}", m);
            _logger.LogInformation(
                "Importador SAP Excel ({Carpeta}): {Equipos} equipos, {Ordenes} órdenes, {Operaciones} operaciones, {Avisos} avisos, {Historial} cambios de estado.",
                _carpeta, datos.Equipos.Count, datos.Ordenes.Count, datos.Operaciones.Count, datos.Avisos.Count, datos.Historial.Count);
            return datos;
        }

        internal static Datos LeerCarpeta(string carpeta, DateTime ahora)
        {
            if (!Directory.Exists(carpeta))
                throw new DirectoryNotFoundException($"No existe la carpeta de exports de SAP '{carpeta}' (SapExcel:Carpeta).");

            var tablas = new List<TablaExcel>();
            var ignorados = new List<string>();
            foreach (var archivo in Directory.EnumerateFiles(carpeta)
                         .Where(f => Path.GetExtension(f).ToLowerInvariant() is ".xlsx" or ".xlsm" or ".txt" or ".csv" or ".tsv")
                         .Where(f => !Path.GetFileName(f).StartsWith("~$"))
                         .OrderBy(f => f))
            {
                var leidas = LectorTablaExcel.Leer(archivo, Conocidos);
                if (leidas.Count == 0) ignorados.Add(Path.GetFileName(archivo));
                tablas.AddRange(leidas);
            }
            if (tablas.Count == 0)
                throw new InvalidOperationException($"La carpeta '{carpeta}' no tiene exports de SAP reconocibles (.xlsx, .txt o .csv con encabezados).");

            var datos = Procesar(tablas, ahora);
            foreach (var nombre in ignorados)
                datos.Mensajes.Add($"'{nombre}' no se reconoció como ningún export: no tiene columnas de SAP conocidas.");
            return datos;
        }

        /// <summary>Tipo de export según sus columnas.</summary>
        internal static string Clasificar(TablaExcel t) =>
            t.Tiene(Objeto) && t.Tiene(Estado) ? "HISTORIAL"
            : t.Tiene(Orden) && t.Tiene(Operacion) && (t.Tiene(Notificacion) || t.Tiene(FechaContabilizacion) || t.Tiene(Anulada)) ? "CONFIRMACIONES"
            : t.Tiene(Orden) && t.Tiene(Operacion) ? "OPERACIONES"
            : t.Tiene(Orden) && t.Tiene(ClaseOrden) ? "ORDENES"
            : t.Tiene(Aviso) && (t.Tiene(ClaseAviso) || t.Tiene(Parada)) ? "AVISOS"
            : t.Tiene(Equipo) && (t.Tiene(CodigoLegado) || t.Tiene(DescripcionEquipo)) && !t.Tiene(Orden) ? "EQUIPOS"
            : "DESCONOCIDO";

        internal static Datos Procesar(IEnumerable<TablaExcel> tablas, DateTime ahora)
        {
            var datos = new Datos();
            var porTipo = tablas.GroupBy(Clasificar).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var t in porTipo.GetValueOrDefault("DESCONOCIDO") ?? new())
                datos.Mensajes.Add($"'{t.Origen}' no se reconoció como ningún export (columnas: {string.Join(", ", t.Encabezados.Take(12))}).");
            foreach (var (tipo, lista) in porTipo.Where(p => p.Key != "DESCONOCIDO"))
                datos.Mensajes.Add($"{tipo}: {string.Join("; ", lista.Select(t => $"'{t.Origen}' ({t.Filas.Count} filas)"))}.");

            var historial = LeerHistorial(porTipo.GetValueOrDefault("HISTORIAL"), datos);
            LeerEquipos(porTipo.GetValueOrDefault("EQUIPOS"), datos);
            LeerOrdenes(porTipo.GetValueOrDefault("ORDENES"), historial, datos, ahora);
            LeerOperaciones(porTipo.GetValueOrDefault("OPERACIONES"), porTipo.GetValueOrDefault("CONFIRMACIONES"), datos, ahora);
            LeerAvisos(porTipo.GetValueOrDefault("AVISOS"), datos, ahora);
            return datos;
        }

        private static Dictionary<string, List<HistorialEstadoDto>> LeerHistorial(List<TablaExcel>? tablas, Datos datos)
        {
            foreach (var f in (tablas ?? new()).SelectMany(t => t.Filas))
            {
                var objeto = f.Texto(Objeto);
                // OBJNR de una orden = "OR" + AUFNR con ceros; los de avisos/equipos no interesan.
                if (objeto == null || !objeto.StartsWith("OR", StringComparison.OrdinalIgnoreCase)) continue;
                var fecha = f.FechaHora(FechaCambio, HoraCambio, TimeSpan.Zero);
                var estado = f.Texto(Estado);
                if (fecha == null || estado == null) continue;
                datos.Historial.Add(new HistorialEstadoDto
                {
                    OrdenSapId = EstadosSap.Id(objeto[2..])!,
                    EstadoCodigo = estado,
                    NumeroCambio = f.Texto(NumeroCambio) ?? "001",
                    FechaHoraCambio = fecha.Value,
                    Indicador = f.Texto(IndicadorCambio) ?? "I"
                });
            }
            // Clave única del staging: (orden, estado, número de cambio).
            var unicos = datos.Historial.GroupBy(h => (h.OrdenSapId, h.EstadoCodigo, h.NumeroCambio)).Select(g => g.First()).ToList();
            datos.Historial.Clear();
            datos.Historial.AddRange(unicos);
            return datos.Historial.GroupBy(h => h.OrdenSapId).ToDictionary(g => g.Key, g => g.ToList());
        }

        private static void LeerEquipos(List<TablaExcel>? tablas, Datos datos)
        {
            if (tablas == null)
            {
                datos.Mensajes.Add("No vino el maestro de equipos (IH08 o Z_PM_MF_WS_MAESTRO_EQUIPO): sin código legado no se puede cruzar con Oracle.");
                return;
            }
            var sinLegado = 0;
            foreach (var f in tablas.SelectMany(t => t.Filas))
            {
                var id = EstadosSap.Id(f.Texto(Equipo));
                if (id == null) continue;
                var legado = f.Texto(CodigoLegado);
                if (legado == null) sinLegado++;
                datos.Equipos.Add(new EquipoSapDto
                {
                    EquipoSapId = id,
                    CodigoLegado = legado,
                    Descripcion = f.Texto(DescripcionEquipo),
                    PuestoTrabajo = f.Texto(PuestoEquipo),
                    Inactivo = EstadosSap.EquipoInactivo(f.Texto(EstadoEquipo))
                });
            }
            var unicos = datos.Equipos.GroupBy(e => e.EquipoSapId).Select(g => g.First()).ToList();
            datos.Equipos.Clear();
            datos.Equipos.AddRange(unicos);
            if (sinLegado > 0)
                datos.Mensajes.Add($"{sinLegado} equipo(s) sin código legado (FLEET_NUM): quedan en \"equipos sin clasificar\".");
        }

        private static void LeerOrdenes(List<TablaExcel>? tablas, Dictionary<string, List<HistorialEstadoDto>> historial, Datos datos, DateTime ahora)
        {
            if (tablas == null)
            {
                datos.Mensajes.Add("No vino el export de órdenes (IW39).");
                return;
            }
            if (!tablas.Any(t => t.Tiene(HoraCreacion)) && historial.Count == 0)
                datos.Mensajes.Add("Las órdenes no traen hora de creación ni historial de estados (JCDS): se asume las 12:00.");

            var sinFecha = 0;
            foreach (var f in tablas.SelectMany(t => t.Filas))
            {
                var id = EstadosSap.Id(f.Texto(Orden));
                if (id == null) continue;
                var cambios = historial.GetValueOrDefault(id) ?? new();

                // La hora exacta sale del historial (alta del estado CREA / CTEC); si no, del export.
                var creacion = PrimerAlta(cambios, EstadosSap.CodigoCreada)
                               ?? f.FechaHora(FechaCreacion, HoraCreacion, HoraSupuesta)
                               ?? f.FechaHora(InicioPlan, HoraInicioPlan, HoraSupuesta)
                               ?? f.FechaHora(InicioReal, HoraInicioReal, HoraSupuesta);
                if (creacion == null)
                {
                    sinFecha++;
                    continue;
                }

                var statusSistema = f.Texto(StatusSistema);
                var estado = EstadosSap.Derivar(statusSistema, f.Texto(StatusUsuario));
                var finReal = f.FechaHora(FinReal, HoraFinReal, HoraSupuesta);
                DateTime? cierre = null;
                if (estado == EstadosSap.Cerrada)
                {
                    cierre = PrimerAlta(cambios, EstadosSap.CodigoCierreTecnico);
                    if (cierre == null && f.Fecha(CierreTecnico) is { } diaCierre)
                        cierre = f.Hora(HoraCierreTecnico) is { } h ? diaCierre.Date + h
                            : finReal?.Date == diaCierre.Date ? finReal
                            : diaCierre.Date + HoraSupuesta;
                    cierre ??= finReal;
                }

                datos.Ordenes.Add(new OrdenSapDto
                {
                    OrdenSapId = id,
                    ClaseOrden = f.Texto(ClaseOrden) ?? "",
                    ClaseActividad = f.Texto(ClaseActividad),
                    EquipoSapId = EstadosSap.Id(f.Texto(Equipo)),
                    Descripcion = Recortar(f.Texto(TextoOrden), 200),
                    PuestoTrabajo = f.Texto(PuestoResponsable),
                    AvisoSapId = EstadosSap.Id(f.Texto(Aviso)),
                    EstadoOrden = estado,
                    StatusSistema = statusSistema,
                    StatusUsuario = f.Texto(StatusUsuario),
                    FechaCreacion = creacion.Value,
                    FechaInicioPlan = f.FechaHora(InicioPlan, HoraInicioPlan, TimeSpan.Zero),
                    FechaFinPlan = f.FechaHora(FinPlan, HoraFinPlan, TimeSpan.Zero),
                    FechaInicioReal = f.FechaHora(InicioReal, HoraInicioReal, HoraSupuesta),
                    FechaFinReal = finReal,
                    FechaCierre = cierre,
                    FechaModificacion = f.Fecha(Modificacion) ?? ahora
                });
            }
            var unicas = datos.Ordenes.GroupBy(o => o.OrdenSapId).Select(g => g.First()).ToList();
            datos.Ordenes.Clear();
            datos.Ordenes.AddRange(unicas);
            if (sinFecha > 0)
                datos.Mensajes.Add($"{sinFecha} orden(es) sin fecha de creación ni de inicio: no se cargaron.");
        }

        private static void LeerOperaciones(List<TablaExcel>? tablas, List<TablaExcel>? confirmaciones, Datos datos, DateTime ahora)
        {
            if (tablas == null)
            {
                datos.Mensajes.Add("No vino el export de operaciones (IW37N): no hay avance por actividades.");
                return;
            }

            // Notificaciones válidas por operación: se descartan las anuladas y sus anulaciones.
            var notif = (confirmaciones ?? new()).SelectMany(t => t.Filas)
                .Where(f => !f.Marca(Anulada) && f.Numero(ContadorAnulacion) == 0)
                .Select(f => (Orden: EstadosSap.Id(f.Texto(Orden)), Op: NormalizarOperacion(f.Texto(Operacion)), Fila: f))
                .Where(x => x.Orden != null && x.Op != null)
                .GroupBy(x => (x.Orden!, x.Op!))
                .ToDictionary(g => g.Key, g => g.Select(x => x.Fila).ToList());
            if (confirmaciones == null && !tablas.Any(t => t.Tiene(UltimaConf)))
                datos.Mensajes.Add("No vinieron las confirmaciones (IW47): la semana de cada actividad terminada sale de su fecha de fin real.");

            foreach (var f in tablas.SelectMany(t => t.Filas))
            {
                var orden = EstadosSap.Id(f.Texto(Orden));
                var op = NormalizarOperacion(f.Texto(Operacion));
                if (orden == null || op == null) continue;

                var conf = notif.GetValueOrDefault((orden, op));
                decimal real;
                DateTime? primera, ultima;
                bool final;
                if (conf is { Count: > 0 })
                {
                    real = conf.Sum(c => Horas(c.Numero(TrabajoReal), c.Texto(UnidadTrabajoReal) ?? c.Texto(UnidadTrabajo)));
                    // La semana en que cuenta es la del día en que se digitó la hora (respuesta del 01/10/2026).
                    var fechas = conf.Select(c => c.Fecha(FechaCaptura) ?? c.Fecha(FechaContabilizacion)).OfType<DateTime>().ToList();
                    primera = fechas.Count > 0 ? fechas.Min() : null;
                    ultima = fechas.Count > 0 ? fechas.Max() : null;
                    final = conf.Any(c => c.Marca(NotificacionFinal));
                }
                else
                {
                    // Sin IW47: el módulo del ABAP trae las fechas ya resumidas; IW37N solo la fecha de fin real.
                    real = Horas(f.Numero(TrabajoReal), f.Texto(UnidadTrabajoReal) ?? f.Texto(UnidadTrabajo));
                    ultima = real > 0 ? f.Fecha(UltimaConf) ?? f.Fecha(FinRealOperacion) : null;
                    primera = real > 0 ? f.Fecha(PrimeraConf) ?? ultima : null;
                    final = f.Valor(ConfFinal) != null ? f.Marca(ConfFinal) : real > 0 && ultima != null;
                }

                datos.Operaciones.Add(new OperacionSapDto
                {
                    OrdenSapId = orden,
                    Operacion = op,
                    Texto = Recortar(f.Texto(TextoOperacion), 200),
                    PuestoTrabajo = f.Texto(PuestoOperacion),
                    Sistema = f.Texto(Sistema),
                    Subsistema = f.Texto(Subsistema),
                    Componente = f.Texto(Componente),
                    TrabajoPlanHh = Horas(f.Numero(Trabajo), f.Texto(UnidadTrabajo)),
                    TrabajoRealHh = real,
                    FechaInicioPlan = f.FechaHora(InicioOperacion, HoraInicioOperacion, TimeSpan.Zero),
                    FechaFinPlan = f.FechaHora(FinOperacion, HoraFinOperacion, TimeSpan.Zero),
                    FechaPrimeraConf = primera,
                    FechaUltimaConf = ultima,
                    ConfFinal = final,
                    UsuarioCreacion = Recortar(f.Texto(CreadoPor), 12),
                    FechaModificacion = ultima ?? ahora
                });
            }
            var unicas = datos.Operaciones.GroupBy(o => (o.OrdenSapId, o.Operacion)).Select(g => g.First()).ToList();
            datos.Operaciones.Clear();
            datos.Operaciones.AddRange(unicas);
        }

        private static void LeerAvisos(List<TablaExcel>? tablas, Datos datos, DateTime ahora)
        {
            if (tablas == null)
            {
                datos.Mensajes.Add("No vino el export de avisos (IW29): las correctivas cuentan desde la creación de la orden.");
                return;
            }
            foreach (var f in tablas.SelectMany(t => t.Filas))
            {
                var id = EstadosSap.Id(f.Texto(Aviso));
                var fecha = f.FechaHora(FechaAviso, HoraAviso, HoraSupuesta);
                if (id == null || fecha == null) continue;
                datos.Avisos.Add(new AvisoSapDto
                {
                    AvisoSapId = id,
                    ClaseAviso = f.Texto(ClaseAviso),
                    Texto = Recortar(f.Texto(TextoAviso), 200),
                    EquipoSapId = EstadosSap.Id(f.Texto(Equipo)),
                    OrdenSapId = EstadosSap.Id(f.Texto(Orden)),
                    FechaAviso = fecha.Value,
                    Parada = f.Marca(Parada),
                    InicioAveria = f.FechaHora(InicioAveria, HoraInicioAveria, TimeSpan.Zero),
                    FinAveria = f.FechaHora(FinAveria, HoraFinAveria, TimeSpan.Zero),
                    StatusSistema = f.Texto(StatusSistema),
                    FechaCierre = f.Fecha(CierreAviso),
                    FechaModificacion = f.Fecha(Modificacion) ?? ahora
                });
            }
            var unicos = datos.Avisos.GroupBy(a => a.AvisoSapId).Select(g => g.First()).ToList();
            datos.Avisos.Clear();
            datos.Avisos.AddRange(unicos);
        }

        // ------------------------------------------------------------ utilidades

        private static DateTime? PrimerAlta(List<HistorialEstadoDto> cambios, string estado) =>
            cambios.Where(c => c.EstadoCodigo.Equals(estado, StringComparison.OrdinalIgnoreCase) && c.Indicador.Equals("I", StringComparison.OrdinalIgnoreCase))
                .Select(c => (DateTime?)c.FechaHoraCambio)
                .Min();

        /// <summary>Trabajo en horas: SAP puede traerlo en minutos o días.</summary>
        internal static decimal Horas(decimal valor, string? unidad) => (unidad ?? "H").Trim().ToUpperInvariant() switch
        {
            "MIN" or "MI" => Math.Round(valor / 60m, 2),
            "D" or "TAG" or "DIA" or "DÍA" => valor * 24m,
            _ => valor
        };

        /// <summary>"10" y "0010" son la misma operación: se guarda con 4 dígitos, como en SAP.</summary>
        private static string? NormalizarOperacion(string? op) =>
            op == null ? null : op.All(char.IsDigit) ? op.PadLeft(4, '0') : op;

        private static string? Recortar(string? texto, int largo) =>
            texto == null || texto.Length <= largo ? texto : texto[..largo];
    }
}
