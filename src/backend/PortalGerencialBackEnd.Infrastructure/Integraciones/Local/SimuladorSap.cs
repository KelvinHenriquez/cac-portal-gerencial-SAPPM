using PortalGerencialBackEnd.Core.Dtos.Sap;

namespace PortalGerencialBackEnd.Infrastructure.Integraciones.Local
{
    /// <summary>
    /// Genera datos de SAP PM verosímiles a partir de la estructura de Oracle, para desarrollar sin
    /// SAP (DataSourceMode=Local). Imita lo que devolverían los módulos del Anexo A v2.
    ///
    /// Es determinista: cada equipo y cada tipo de orden usan su propia semilla, y todo lo aleatorio
    /// de una orden se sortea antes de mirar la fecha de hoy. El mismo día siempre produce los mismos
    /// datos y, con los días, las órdenes avanzan solas (creada → liberada → en proceso → cerrada).
    ///
    /// Qué simula, desde la salida en vivo de SAP (01/06/2026):
    /// - ZC03 (programa de reparación): una por equipo activo y por programa de su familia, con ~20
    ///   actividades (operaciones) con sistema / sub-sistema / componente; ~15 % agregadas en la
    ///   reparación (no programadas).
    /// - ZC02 (preventivo): cada 35-60 días, de 4 a 10 horas.
    /// - ZC01 (correctivo): averías aleatorias con su aviso; el 75 % de los avisos indican parada.
    /// - Brechas reales (sección 6.3): ~5 % de reparaciones terminadas sin cierre técnico y ~15 % sin
    ///   horas confirmadas; tres equipos que solo existen en SAP.
    /// </summary>
    public static class SimuladorSap
    {
        public const string Creada = "Creada";
        public const string Liberada = "Liberada";
        public const string EnProceso = "En Proceso";
        public const string Cerrada = "Cerrada";

        /// <summary>Salida en vivo de SAP PM: antes no hay órdenes.</summary>
        public static readonly DateTime InicioSap = new(2026, 6, 1);

        public record EquipoOracle(string CodigoLegado, string Descripcion, string FamiliaCodigo, bool Activo);
        /// <summary>
        /// Programa de una familia en un reporte de avance: su ventana y los equipos (códigos legados)
        /// de las OT asignadas; Equipos null = todos los de la familia.
        /// </summary>
        public record ProgramaFamilia(int ReporteId, string FamiliaCodigo, DateTime FechaEstInicio, DateTime FechaEstFin, ISet<string>? Equipos = null);

        public sealed class Datos
        {
            public List<EquipoSapDto> Equipos { get; } = new();
            public List<OrdenSapDto> Ordenes { get; } = new();
            public List<OperacionSapDto> Operaciones { get; } = new();
            public List<AvisoSapDto> Avisos { get; } = new();
            public List<HistorialEstadoDto> Historial { get; } = new();
        }

        private static readonly (string Sistema, string Subsistema, string[] Componentes)[] Catalogo =
        {
            ("Potencia", "Motor", new[] { "Block", "Bomba de inyección", "Bomba de agua", "Culata", "Cigüeñal", "Inyectores" }),
            ("Potencia", "Enfriamiento", new[] { "Radiador", "Termostato", "Ventilador" }),
            ("Tracción", "Transmisión", new[] { "Caja de cambios", "Embrague", "Diferencial" }),
            ("Tracción", "Rodaje", new[] { "Ejes", "Mazas", "Neumáticos" }),
            ("Eléctrico", "Arranque", new[] { "Motor de arranque", "Alternador", "Baterías" }),
            ("Eléctrico", "Luces", new[] { "Luces delanteras", "Luces traseras", "Tablero" }),
            ("Estructura", "Chasis", new[] { "Bastidor", "Soldaduras", "Enganche" }),
            ("Componentes Genéricos", "Hidráulico", new[] { "Bomba hidráulica", "Mangueras", "Cilindros" })
        };

        private static readonly string[] AccionesActividad = { "Revisar", "Reparar", "Cambiar", "Ajustar" };
        private static readonly string[] MotivosAveria =
        {
            "Pérdida de presión", "Recalentamiento", "Fuga de aceite", "Falla eléctrica", "Rotura de manguera", "Ruido anormal"
        };

        public static Datos Generar(IEnumerable<EquipoOracle> equiposOracle, IEnumerable<ProgramaFamilia> programas, DateTime ahora)
        {
            var datos = new Datos();
            var programasPorFamilia = programas.Where(p => p.FechaEstFin >= InicioSap).ToLookup(p => p.FamiliaCodigo);

            var indice = 0;
            foreach (var e in equiposOracle.OrderBy(e => e.CodigoLegado, StringComparer.Ordinal))
            {
                indice++;
                var equipoSapId = (10000000 + indice).ToString();
                var puesto = "TALLER-" + e.FamiliaCodigo[..Math.Min(8, e.FamiliaCodigo.Length)];
                datos.Equipos.Add(new EquipoSapDto { EquipoSapId = equipoSapId, CodigoLegado = e.CodigoLegado, Descripcion = e.Descripcion, PuestoTrabajo = puesto });

                if (!e.Activo)
                    continue;

                foreach (var p in programasPorFamilia[e.FamiliaCodigo].Where(p => p.Equipos == null || p.Equipos.Contains(e.CodigoLegado)))
                    GenerarReparacion(datos, new Random(Semilla($"{e.CodigoLegado}|ZC03|{p.ReporteId}")), indice, equipoSapId, e, puesto, p, ahora);

                GenerarPreventivos(datos, new Random(Semilla($"{e.CodigoLegado}|ZC02")), indice, equipoSapId, puesto, ahora);
                var esRiego = e.FamiliaCodigo is "BOMBAS_RIEGO" or "POZOS" or "TURBINAS";
                GenerarCorrectivos(datos, new Random(Semilla($"{e.CodigoLegado}|ZC01")), indice, equipoSapId, puesto, ahora, esRiego ? 0.012 : 0.006);
            }

            // Equipos que solo existen en SAP (sección 10: no deben perderse en silencio).
            var soloSap = new[]
            {
                ("99000001", (string?)null, "Bomba de riego Pivote 3"),
                ("99000002", (string?)"TR-900", "Tractor sin ficha en Oracle"),
                ("99000003", (string?)"XX-001", "Planta eléctrica taller")
            };
            var extra = 9000;
            foreach (var (id, legado, desc) in soloSap)
            {
                extra++;
                datos.Equipos.Add(new EquipoSapDto { EquipoSapId = id, CodigoLegado = legado, Descripcion = desc, PuestoTrabajo = "TALLER-GENERAL" });
                GenerarCorrectivos(datos, new Random(Semilla(id)), extra, id, "TALLER-GENERAL", ahora, 0.02);
            }

            return datos;
        }

        // ------------------------------------------------------------ ZC03 programa de reparación

        private static void GenerarReparacion(Datos datos, Random rng, int indice, string equipoSapId, EquipoOracle equipo,
            string puesto, ProgramaFamilia programa, DateTime ahora)
        {
            // Todas las tiradas primero (determinismo: no dependen de hoy).
            var u = (rng.NextDouble() + rng.NextDouble()) / 2;
            var duracionPlan = rng.Next(10, 36);
            var atraso = rng.Next(-2, 8);
            var factorDuracion = 0.8 + rng.NextDouble() * 0.6;
            var nuncaSeCerro = rng.NextDouble() < 0.05;
            var sinConfirmaciones = rng.NextDouble() < 0.15;
            var horaCreacion = rng.Next(7, 16);
            var horasCierre = rng.Next(2, 30);
            var cantActividades = rng.Next(15, 26);
            var actividades = Enumerable.Range(0, cantActividades).Select(_ =>
            {
                var c = Catalogo[rng.Next(Catalogo.Length)];
                return (Accion: AccionesActividad[rng.Next(AccionesActividad.Length)], c.Sistema, c.Subsistema,
                        Componente: c.Componentes[rng.Next(c.Componentes.Length)],
                        PlanHh: (decimal)rng.Next(1, 9), Factor: 0.7 + rng.NextDouble() * 0.6,
                        Programada: rng.NextDouble() >= 0.15, MinutosConf: rng.Next(60, 600));
            }).ToList();

            var largo = (programa.FechaEstFin - programa.FechaEstInicio).TotalDays;
            var inicioPlan = programa.FechaEstInicio.Date.AddDays(Math.Floor(u * largo * 0.75)).AddHours(7);
            var finPlan = inicioPlan.AddDays(duracionPlan);
            var creacion = inicioPlan.Date.AddDays(-14).AddHours(horaCreacion).AddMinutes(indice % 60);
            if (creacion > ahora || creacion < InicioSap)
                return; // todavía no existe en SAP, o es de antes de la salida en vivo

            var liberacion = inicioPlan.AddDays(-7) < creacion ? creacion.AddHours(1) : inicioPlan.AddDays(-7);
            var inicioReal = inicioPlan.AddDays(atraso);
            var finReal = inicioReal.AddDays(Math.Ceiling(duracionPlan * factorDuracion)).AddHours(3);
            var cierre = finReal.AddHours(horasCierre);

            string estado;
            DateTime? inicioRealOrden = null, finRealOrden = null, cierreOrden = null;
            if (ahora < liberacion) estado = Creada;
            else if (ahora < inicioReal) estado = Liberada;
            else if (ahora < cierre || nuncaSeCerro) { estado = EnProceso; inicioRealOrden = inicioReal; if (ahora >= finReal && nuncaSeCerro) finRealOrden = null; }
            else { estado = Cerrada; inicioRealOrden = inicioReal; finRealOrden = finReal; cierreOrden = cierre; }

            var ordenId = $"4{indice:D4}{programa.ReporteId:D2}0";
            var eventos = new List<(string Codigo, string Texto, DateTime Fecha, string Ind)> { ("I0001", "CREA", creacion, "I") };
            if (ahora >= liberacion) eventos.Add(("I0002", "LIB.", liberacion, "I"));
            if (inicioRealOrden != null) eventos.Add(("E0002", "PROC", inicioReal, "I"));
            if (cierreOrden != null) { eventos.Add(("E0002", "PROC", cierre, "D")); eventos.Add(("I0045", "CTEC", cierre, "I")); }

            // Actividades: las programadas reparten la ventana plan; las agregadas caen en la segunda
            // mitad. Cada una se termina (y se confirman sus horas) a medida que avanza la reparación.
            var programadas = actividades.Count(a => a.Programada);
            var duracionReal = finReal - inicioReal;
            var modificacionOps = creacion;
            var iProg = 0;
            for (var k = 0; k < actividades.Count; k++)
            {
                var a = actividades[k];
                DateTime ini, fin;
                if (a.Programada)
                {
                    var paso = (finPlan - inicioPlan).TotalHours / Math.Max(1, programadas);
                    ini = inicioPlan.AddHours(paso * iProg);
                    fin = ini.AddHours(Math.Max(4, paso));
                    iProg++;
                }
                else
                {
                    ini = inicioPlan.AddHours((finPlan - inicioPlan).TotalHours * (0.5 + 0.4 * k / actividades.Count));
                    fin = ini.AddHours(8);
                }

                var terminaEn = inicioReal.AddHours(duracionReal.TotalHours * (k + 1) / actividades.Count).AddMinutes(a.MinutosConf);
                var terminada = !sinConfirmaciones && inicioRealOrden != null && terminaEn <= ahora;
                if (terminada && terminaEn > modificacionOps) modificacionOps = terminaEn;

                datos.Operaciones.Add(new OperacionSapDto
                {
                    OrdenSapId = ordenId,
                    Operacion = ((k + 1) * 10).ToString("D4"),
                    Texto = a.Accion,
                    PuestoTrabajo = puesto,
                    Sistema = a.Sistema,
                    Subsistema = a.Subsistema,
                    Componente = a.Componente,
                    TrabajoPlanHh = a.PlanHh,
                    TrabajoRealHh = terminada ? Math.Round(a.PlanHh * (decimal)a.Factor, 2) : 0m,
                    FechaInicioPlan = ini,
                    FechaFinPlan = fin,
                    FechaPrimeraConf = terminada ? terminaEn : null,
                    FechaUltimaConf = terminada ? terminaEn : null,
                    ConfFinal = terminada,
                    Programada = a.Programada,
                    // Las de la hoja de ruta las crea el usuario de planificación; las agregadas, el taller.
                    UsuarioCreacion = a.Programada ? "PLANIFICACION" : $"TALLER{indice % 3 + 1}",
                    FechaModificacion = terminada ? terminaEn : creacion
                });
            }

            var modificacion = eventos.Max(ev => ev.Fecha);
            datos.Ordenes.Add(new OrdenSapDto
            {
                OrdenSapId = ordenId,
                ClaseOrden = "ZC03",
                ClaseActividad = "PRG",
                EquipoSapId = equipoSapId,
                Descripcion = $"Reparación interzafra {equipo.CodigoLegado}",
                PuestoTrabajo = puesto,
                EstadoOrden = estado,
                StatusSistema = EstadoATexto(estado),
                FechaCreacion = creacion,
                FechaInicioPlan = inicioPlan,
                FechaFinPlan = finPlan,
                FechaInicioReal = inicioRealOrden,
                FechaFinReal = finRealOrden,
                FechaCierre = cierreOrden,
                FechaModificacion = modificacionOps > modificacion ? modificacionOps : modificacion
            });
            AgregarHistorial(datos, ordenId, eventos);
        }

        // ------------------------------------------------------------ ZC02 preventivo

        private static void GenerarPreventivos(Datos datos, Random rng, int indice, string equipoSapId, string puesto, DateTime ahora)
        {
            var intervalo = rng.Next(35, 61);
            var dia = InicioSap.AddDays(rng.Next(0, intervalo));
            var secuencia = 0;
            while (dia <= ahora)
            {
                var hora = rng.Next(6, 9);
                var horas = rng.Next(4, 11);
                var (sistema, subsistema, componentes) = Catalogo[rng.Next(Catalogo.Length)];
                var componente = componentes[rng.Next(componentes.Length)];

                secuencia++;
                var creacion = dia.AddHours(hora);
                if (creacion > ahora) break;
                var cierre = creacion.AddHours(horas);
                var cerrada = cierre <= ahora;
                var ordenId = $"2{indice:D4}{secuencia:D3}";

                datos.Ordenes.Add(new OrdenSapDto
                {
                    OrdenSapId = ordenId,
                    ClaseOrden = "ZC02",
                    ClaseActividad = "PRV",
                    EquipoSapId = equipoSapId,
                    Descripcion = "Mantenimiento preventivo",
                    PuestoTrabajo = puesto,
                    EstadoOrden = cerrada ? Cerrada : EnProceso,
                    StatusSistema = EstadoATexto(cerrada ? Cerrada : EnProceso),
                    FechaCreacion = creacion,
                    FechaInicioPlan = creacion,
                    FechaFinPlan = cierre,
                    FechaInicioReal = creacion,
                    FechaFinReal = cerrada ? cierre : null,
                    FechaCierre = cerrada ? cierre : null,
                    FechaModificacion = cerrada ? cierre : creacion
                });
                datos.Operaciones.Add(new OperacionSapDto
                {
                    OrdenSapId = ordenId,
                    Operacion = "0010",
                    Texto = "Revisar",
                    PuestoTrabajo = puesto,
                    Sistema = sistema,
                    Subsistema = subsistema,
                    Componente = componente,
                    TrabajoPlanHh = horas,
                    TrabajoRealHh = cerrada ? horas : 0,
                    FechaInicioPlan = creacion,
                    FechaFinPlan = cierre,
                    FechaPrimeraConf = cerrada ? cierre : null,
                    FechaUltimaConf = cerrada ? cierre : null,
                    ConfFinal = cerrada,
                    FechaModificacion = cerrada ? cierre : creacion
                });
                var eventos = new List<(string, string, DateTime, string)> { ("I0001", "CREA", creacion, "I"), ("I0002", "LIB.", creacion, "I") };
                if (cerrada) eventos.Add(("I0045", "CTEC", cierre, "I"));
                AgregarHistorial(datos, ordenId, eventos);

                dia = dia.AddDays(intervalo);
            }
        }

        // ------------------------------------------------------------ ZC01 correctivo + aviso

        private static void GenerarCorrectivos(Datos datos, Random rng, int indice, string equipoSapId, string puesto, DateTime ahora, double probabilidad)
        {
            var secuencia = 0;
            for (var dia = InicioSap; dia <= ahora.Date; dia = dia.AddDays(1))
            {
                if (rng.NextDouble() >= probabilidad)
                    continue;

                var falla = dia.AddHours(rng.Next(0, 24)).AddMinutes(rng.Next(0, 60));
                var aviso = falla.AddMinutes(rng.Next(10, 120));
                var creacion = aviso.AddMinutes(rng.Next(15, 180));
                var horasReparacion = rng.Next(4, 73);
                var parada = rng.NextDouble() < 0.75;
                var motivo = MotivosAveria[rng.Next(MotivosAveria.Length)];
                var (sistema, subsistema, componentes) = Catalogo[rng.Next(Catalogo.Length)];
                var componente = componentes[rng.Next(componentes.Length)];
                if (falla > ahora)
                    break;

                secuencia++;
                var cierre = creacion.AddHours(horasReparacion);
                var cerrada = cierre <= ahora && creacion <= ahora;
                var ordenId = $"3{indice:D4}{secuencia:D3}";
                var avisoId = $"1{indice:D4}{secuencia:D3}";

                datos.Avisos.Add(new AvisoSapDto
                {
                    AvisoSapId = avisoId,
                    ClaseAviso = "M2",
                    Texto = motivo,
                    EquipoSapId = equipoSapId,
                    OrdenSapId = creacion <= ahora ? ordenId : null,
                    FechaAviso = aviso,
                    Parada = parada,
                    InicioAveria = parada ? falla : null,
                    FinAveria = parada && cerrada ? cierre.AddMinutes(-30) : null,
                    StatusSistema = cerrada ? "MECE" : "MEAB",
                    FechaCierre = cerrada ? cierre : null,
                    FechaModificacion = cerrada ? cierre : aviso
                });

                if (creacion <= ahora)
                {
                    datos.Ordenes.Add(new OrdenSapDto
                    {
                        OrdenSapId = ordenId,
                        ClaseOrden = "ZC01",
                        ClaseActividad = "COR",
                        EquipoSapId = equipoSapId,
                        Descripcion = $"Correctivo: {motivo}",
                        PuestoTrabajo = puesto,
                        AvisoSapId = avisoId,
                        EstadoOrden = cerrada ? Cerrada : EnProceso,
                        StatusSistema = EstadoATexto(cerrada ? Cerrada : EnProceso),
                        FechaCreacion = creacion,
                        FechaInicioPlan = creacion,
                        FechaFinPlan = cierre,
                        FechaInicioReal = creacion.AddMinutes(30),
                        FechaFinReal = cerrada ? cierre : null,
                        FechaCierre = cerrada ? cierre : null,
                        FechaModificacion = cerrada ? cierre : creacion
                    });
                    datos.Operaciones.Add(new OperacionSapDto
                    {
                        OrdenSapId = ordenId,
                        Operacion = "0010",
                        Texto = "Reparar",
                        PuestoTrabajo = puesto,
                        Sistema = sistema,
                        Subsistema = subsistema,
                        Componente = componente,
                        TrabajoPlanHh = Math.Min(horasReparacion, 16),
                        TrabajoRealHh = cerrada ? Math.Min(horasReparacion, 16) : 0,
                        FechaInicioPlan = creacion,
                        FechaFinPlan = cierre,
                        FechaPrimeraConf = cerrada ? cierre : null,
                        FechaUltimaConf = cerrada ? cierre : null,
                        ConfFinal = cerrada,
                        FechaModificacion = cerrada ? cierre : creacion
                    });
                    var eventos = new List<(string, string, DateTime, string)> { ("I0001", "CREA", creacion, "I"), ("I0002", "LIB.", creacion, "I") };
                    if (cerrada) eventos.Add(("I0045", "CTEC", cierre, "I"));
                    AgregarHistorial(datos, ordenId, eventos);
                }

                dia = cierre.Date; // no se vuelve a averiar mientras está en reparación
            }
        }

        // ------------------------------------------------------------ apoyo

        private static string EstadoATexto(string estado) => estado switch
        {
            Creada => "CREA",
            Liberada => "LIB.",
            EnProceso => "LIB. NOTP",
            Cerrada => "CTEC",
            _ => estado
        };

        private static void AgregarHistorial(Datos datos, string ordenId, List<(string Codigo, string Texto, DateTime Fecha, string Ind)> eventos)
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
                        FechaHoraCambio = e.Fecha,
                        Indicador = e.Ind
                    });
                }
            }
        }

        /// <summary>FNV-1a: string.GetHashCode cambia en cada proceso de .NET, esto no.</summary>
        private static int Semilla(string texto)
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
