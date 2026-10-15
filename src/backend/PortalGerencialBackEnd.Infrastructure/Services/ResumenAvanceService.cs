using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Dtos.Auth;
using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Dtos.ResumenAvance;
using PortalGerencialBackEnd.Core.Entities;
using PortalGerencialBackEnd.Core.Entities.Calculo;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    /// <summary>
    /// "Resumen de avance de reparación" (pedido del Taller, 14/10/2026): la lámina semanal que hoy
    /// arma Arturo a mano para el gerente agrícola. Una fila por familia de un programa de Oracle,
    /// agrupadas (Agrícola, Automotriz, Ferroviaria...), con el % programado y ejecutado de la semana y
    /// acumulado (los mismos cálculos del reporte de Avance) y la maquinaria entregada.
    /// </summary>
    public class ResumenAvanceService : IResumenAvanceService
    {
        private readonly PortalGerencialDbContext _db;
        private readonly IStructureSource _estructura;
        private readonly ISistemaService _sistema;
        private readonly TimeProvider _time;

        public ResumenAvanceService(PortalGerencialDbContext db, IStructureSource estructura, ISistemaService sistema, TimeProvider time)
        {
            _db = db;
            _estructura = estructura;
            _sistema = sistema;
            _time = time;
        }

        // ------------------------------------------------------------ resumen

        public async Task<ResumenAvanceDto> GetResumenAsync(string area, DateTime? semanaFin, string medida, PermisosDto permisos)
        {
            area = area.ToUpperInvariant();
            medida = string.Equals(medida, ReporteAvanceService.Actividades, StringComparison.OrdinalIgnoreCase)
                ? ReporteAvanceService.Actividades : ReporteAvanceService.Ordenes;
            var corte = (await _sistema.GetEstadoActualizacionAsync()).UltimaActualizacion;
            var hoy = (corte ?? _time.GetLocalNow().DateTime).Date;

            var configs = await ConfigsDelPeriodoAsync();
            var (filasConfig, guardada) = await FilasAsync(area, configs);
            var porReporte = configs.ToDictionary(c => c.ReporteId);
            var filasValidas = filasConfig.Where(f => porReporte.TryGetValue(f.ReporteId, out var c)
                                                      && c.Familias.Any(x => x.FamiliaCodigo == f.FamiliaCodigo)).ToList();

            // Calendario de referencia: el del programa con más filas (el principal).
            var referencia = filasValidas.GroupBy(f => f.ReporteId).OrderByDescending(g => g.Count()).Select(g => porReporte[g.Key]).FirstOrDefault()
                             ?? configs.OrderByDescending(c => c.Familias.Count).FirstOrDefault();
            var semanas = (referencia?.Familias.SelectMany(f => f.Semanas) ?? Enumerable.Empty<SemanaPlanDto>())
                .GroupBy(s => s.NoSemana).Select(g => g.First()).OrderBy(s => s.NoSemana)
                .Select(s => new SemanaResumenDto
                {
                    NoSemana = s.NoSemana,
                    FechaInicio = s.FechaInicio,
                    FechaFin = s.FechaFin,
                    Momento = s.FechaFin < hoy ? "PASADA" : s.FechaInicio <= hoy ? "ACTUAL" : "FUTURA"
                }).ToList();

            var resumen = new ResumenAvanceDto
            {
                Area = area,
                Medida = medida,
                FechaCorte = corte,
                Periodo = referencia?.PeriodoDescripcion,
                PuedeEditar = permisos.AreasEditables.Contains(area, StringComparer.OrdinalIgnoreCase),
                ConfiguracionGuardada = guardada,
                Semanas = semanas
            };
            if (semanas.Count == 0)
                return resumen;

            var semana = semanaFin is { } sf
                ? semanas.FirstOrDefault(s => s.FechaFin.Date == sf.Date) ?? throw new NotFoundException("Esa semana no está en el calendario del programa.")
                : semanas.FirstOrDefault(s => s.Momento == "ACTUAL") ?? semanas.LastOrDefault(s => s.Momento == "PASADA") ?? semanas[0];
            resumen.NoSemana = semana.NoSemana;
            resumen.SemanaInicio = semana.FechaInicio;
            resumen.SemanaFin = semana.FechaFin;
            var finSemana = semana.FechaFin.Date;

            var calc = (await _db.CalcAvanceSemanal.AsNoTracking()
                    .Where(a => filasValidas.Select(f => f.ReporteId).Distinct().Contains(a.ReporteId)).ToListAsync())
                .ToLookup(a => (a.ReporteId, a.FamiliaCodigo));
            var notas = (await _db.ResumenAvanceNotas.AsNoTracking()
                    .Where(n => n.Area == area && n.SemanaFin == finSemana).ToListAsync())
                .ToDictionary(n => (n.ReporteId, n.FamiliaCodigo));
            var entregados = await EntregadosAsync(filasValidas, porReporte, finSemana);

            decimal Real(CalcAvanceSemanal a) => medida == ReporteAvanceService.Ordenes ? a.RealOrdenesPct : a.RealActPct;

            foreach (var grupo in filasValidas.OrderBy(f => f.Orden).GroupBy(f => f.Grupo))
            {
                var filas = new List<FilaResumenDto>();
                foreach (var f in grupo)
                {
                    var familia = porReporte[f.ReporteId].Familias.First(x => x.FamiliaCodigo == f.FamiliaCodigo);
                    var semanasFamilia = calc[(f.ReporteId, f.FamiliaCodigo)].OrderBy(a => a.NoSemana).ToList();
                    // La semana de ESE programa que contiene el fin de la semana del resumen (los calendarios pueden diferir).
                    var actual = semanasFamilia.FirstOrDefault(a => a.FechaInicio.Date <= finSemana && finSemana <= a.FechaFin.Date);
                    var hasta = semanasFamilia.Where(a => a.FechaInicio.Date <= finSemana).ToList();
                    var conReal = hasta.Where(a => a.FechaInicio.Date <= hoy).ToList();
                    var prog = hasta.Sum(a => a.ProgPct);
                    var ejec = conReal.Sum(Real);
                    var nota = notas.GetValueOrDefault((f.ReporteId, f.FamiliaCodigo));
                    var (programados, entregadosFila) = entregados.GetValueOrDefault((f.ReporteId, f.FamiliaCodigo));
                    filas.Add(new FilaResumenDto
                    {
                        ReporteId = f.ReporteId,
                        FamiliaCodigo = f.FamiliaCodigo,
                        Nombre = f.Nombre,
                        Cantidad = familia.CantEquipo,
                        ProgSemanaPct = actual == null ? null : Math.Round(actual.ProgPct, 2),
                        EjecSemanaPct = actual == null || actual.FechaInicio.Date > hoy ? null : Math.Round(Real(actual), 2),
                        ProgAcumPct = Math.Round(prog, 2),
                        EjecAcumPct = Math.Round(ejec, 2),
                        DiferenciaPct = Math.Round(ejec - prog, 2),
                        ProgramadosEntregar = programados,
                        Entregados = entregadosFila,
                        Nota = nota?.Texto,
                        NotaPor = nota?.ActualizadoPor,
                        NotaEn = nota?.ActualizadoEn
                    });
                }
                resumen.Grupos.Add(new GrupoResumenDto { Nombre = grupo.Key, Filas = filas, General = General(grupo.Key, filas) });
            }
            return resumen;
        }

        /// <summary>
        /// Fila "General" del grupo: cantidades sumadas y porcentajes PONDERADOS por cantidad de
        /// máquinas (la lámina sumaba los % de las familias y daba más de 100 %).
        /// </summary>
        public static FilaResumenDto General(string grupo, IReadOnlyCollection<FilaResumenDto> filas)
        {
            var total = filas.Sum(f => f.Cantidad);
            decimal? Pond(Func<FilaResumenDto, decimal?> valor)
            {
                var con = filas.Where(f => valor(f) != null).ToList();
                var peso = con.Sum(f => f.Cantidad);
                return peso == 0 ? null : Math.Round(con.Sum(f => f.Cantidad * valor(f)!.Value) / peso, 2);
            }
            var prog = Pond(f => f.ProgAcumPct) ?? 0;
            var ejec = Pond(f => f.EjecAcumPct) ?? 0;
            return new FilaResumenDto
            {
                Nombre = $"General {grupo}",
                FamiliaCodigo = "",
                Cantidad = total,
                ProgSemanaPct = Pond(f => f.ProgSemanaPct),
                EjecSemanaPct = Pond(f => f.EjecSemanaPct),
                ProgAcumPct = prog,
                EjecAcumPct = ejec,
                DiferenciaPct = Math.Round(ejec - prog, 2),
                ProgramadosEntregar = filas.Sum(f => f.ProgramadosEntregar),
                Entregados = filas.Sum(f => f.Entregados)
            };
        }

        /// <summary>
        /// Por fila: equipos programados a entregar hasta esa semana (última actividad planificada de
        /// su OT en Oracle) y entregados (todas sus órdenes de reparación de SAP del programa
        /// terminadas, con la misma regla del Avance, hasta el fin de la semana).
        /// </summary>
        private async Task<Dictionary<(int, string), (int Programados, int Entregados)>> EntregadosAsync(
            List<FilaConfigResumenDto> filas, Dictionary<int, ConfigAvanceDto> porReporte, DateTime finSemana)
        {
            var parametros = await ParametrosPortal.CargarAsync(_db);
            var reglas = ReglasPortal.Ventana(parametros);
            var clases = ReglasPortal.ClasesAvance(parametros).ToList();
            var mapeo = await MapeoEquipos.CargarAsync(_db, _estructura);

            var programas = filas.Select(f =>
            {
                var config = porReporte[f.ReporteId];
                var familia = config.Familias.First(x => x.FamiliaCodigo == f.FamiliaCodigo);
                return (Fila: f, Config: config, Familia: familia, Equipos: ReglasPortal.EquiposDelPrograma(familia, mapeo));
            }).ToList();

            var equiposSap = programas.SelectMany(p => p.Equipos)
                .Where(mapeo.EquipoSapPorClave.ContainsKey).Select(l => mapeo.EquipoSapPorClave[l]).Distinct().ToList();
            var ordenes = new List<Core.Entities.Sap.StgSapOrden>();
            foreach (var lote in equiposSap.Chunk(900))
                ordenes.AddRange(await _db.StgSapOrdenes.AsNoTracking()
                    .Where(o => o.EquipoSapId != null && lote.Contains(o.EquipoSapId) && clases.Contains(o.ClaseOrden)).ToListAsync());

            var resultado = new Dictionary<(int, string), (int, int)>();
            foreach (var p in programas)
            {
                // Semana de ESE programa que contiene el fin de la semana del resumen.
                var semanaPrograma = p.Familia.Semanas.FirstOrDefault(s => s.FechaInicio <= finSemana && finSemana <= s.FechaFin)?.NoSemana
                                     ?? (p.Familia.Semanas.Count > 0 && finSemana > p.Familia.Semanas.Max(s => s.FechaFin) ? int.MaxValue : 0);
                var programados = p.Familia.Ordenes
                    .Where(o => o.SemanaFin is { } s ? s <= semanaPrograma : p.Familia.FechaEstFin <= finSemana)
                    .Select(o => o.CodigoLegado).Distinct(StringComparer.OrdinalIgnoreCase).Count();

                var delPrograma = ordenes.Where(o => ReglasPortal.EsDelPrograma(o, mapeo, p.Config, p.Equipos, reglas)).ToList();
                var entregados = delPrograma.GroupBy(o => o.EquipoSapId)
                    .Count(g => g.All(o => ReglasPortal.OrdenTerminada(o, reglas) is { } fin && fin.Date <= finSemana));
                resultado[(p.Fila.ReporteId, p.Fila.FamiliaCodigo)] = (programados, entregados);
            }
            return resultado;
        }

        // ------------------------------------------------------------ configuración

        public async Task<ConfigResumenDto> GetConfigAsync(string area)
        {
            var configs = await ConfigsDelPeriodoAsync();
            var (filas, guardada) = await FilasAsync(area.ToUpperInvariant(), configs);
            return new ConfigResumenDto
            {
                Guardada = guardada,
                Filas = filas,
                Opciones = configs.SelectMany(c => c.Familias.Select(f => new OpcionResumenDto
                {
                    ReporteId = c.ReporteId,
                    ReporteNombre = c.Nombre,
                    FamiliaCodigo = f.FamiliaCodigo,
                    FamiliaNombre = f.FamiliaNombre,
                    CantEquipo = f.CantEquipo
                })).ToList()
            };
        }

        public async Task<ConfigResumenDto> GuardarConfigAsync(string area, List<FilaConfigResumenDto> filas, string usuario, PermisosDto permisos)
        {
            area = area.ToUpperInvariant();
            ExigirEdicion(area, permisos);
            var configs = await ConfigsDelPeriodoAsync();
            var validas = configs.SelectMany(c => c.Familias.Select(f => (c.ReporteId, f.FamiliaCodigo))).ToHashSet();
            if (filas.Count == 0)
                throw new BadRequestException("El resumen necesita al menos una fila.");
            foreach (var f in filas)
            {
                if (string.IsNullOrWhiteSpace(f.Grupo) || string.IsNullOrWhiteSpace(f.Nombre))
                    throw new BadRequestException("Cada fila necesita grupo y nombre.");
                if (!validas.Contains((f.ReporteId, f.FamiliaCodigo)))
                    throw new BadRequestException($"La familia {f.FamiliaCodigo} no está en el programa {f.ReporteId}.");
            }
            if (filas.GroupBy(f => (f.ReporteId, f.FamiliaCodigo)).Any(g => g.Count() > 1))
                throw new BadRequestException("Una misma familia de un programa no puede ir en dos filas.");

            var ahora = _time.GetLocalNow().DateTime;
            await _db.ResumenAvanceFilas.Where(f => f.Area == area).ExecuteDeleteAsync();
            var orden = 0;
            _db.ResumenAvanceFilas.AddRange(filas.Select(f => new ResumenAvanceFila
            {
                Area = area,
                ReporteId = f.ReporteId,
                FamiliaCodigo = f.FamiliaCodigo,
                Grupo = f.Grupo.Trim()[..Math.Min(f.Grupo.Trim().Length, 100)],
                Nombre = f.Nombre.Trim()[..Math.Min(f.Nombre.Trim().Length, 100)],
                Orden = ++orden,
                ActualizadoPor = usuario.ToUpperInvariant(),
                ActualizadoEn = ahora
            }));
            await _db.SaveChangesAsync();
            return await GetConfigAsync(area);
        }

        public async Task GuardarNotaAsync(string area, GuardarNotaResumenRequest nota, string usuario, PermisosDto permisos)
        {
            area = area.ToUpperInvariant();
            ExigirEdicion(area, permisos);
            var fin = nota.SemanaFin.Date;
            var existentes = await _db.ResumenAvanceNotas
                .Where(n => n.Area == area && n.ReporteId == nota.ReporteId && n.FamiliaCodigo == nota.FamiliaCodigo && n.SemanaFin == fin)
                .ToListAsync();
            var texto = (nota.Texto ?? "").Trim();
            if (texto.Length == 0)
                _db.ResumenAvanceNotas.RemoveRange(existentes);
            else
            {
                var n = existentes.FirstOrDefault();
                if (n == null)
                {
                    n = new ResumenAvanceNota { Area = area, ReporteId = nota.ReporteId, FamiliaCodigo = nota.FamiliaCodigo, SemanaFin = fin };
                    _db.ResumenAvanceNotas.Add(n);
                }
                n.Texto = texto[..Math.Min(texto.Length, 300)];
                n.ActualizadoPor = usuario.ToUpperInvariant();
                n.ActualizadoEn = _time.GetLocalNow().DateTime;
            }
            await _db.SaveChangesAsync();
        }

        private static void ExigirEdicion(string area, PermisosDto permisos)
        {
            if (!permisos.AreasEditables.Contains(area, StringComparer.OrdinalIgnoreCase))
                throw new ForbiddenException("Solo la gente del área (o TI) puede configurar el resumen y escribir notas.");
        }

        // ------------------------------------------------------------ apoyo

        /// <summary>Programas de avance del período más reciente (los que se están reparando).</summary>
        private async Task<List<ConfigAvanceDto>> ConfigsDelPeriodoAsync()
        {
            var todos = await _estructura.GetConfigsAvanceAsync();
            if (todos.Count == 0) return todos;
            var periodo = todos.Max(c => c.PeriodoId);
            return todos.Where(c => c.PeriodoId == periodo).ToList();
        }

        private async Task<(List<FilaConfigResumenDto> Filas, bool Guardada)> FilasAsync(string area, List<ConfigAvanceDto> configs)
        {
            var guardadas = await _db.ResumenAvanceFilas.AsNoTracking().Where(f => f.Area == area).ToListAsync();
            if (guardadas.Count > 0)
                return (guardadas.OrderBy(f => f.Orden).Select(f => new FilaConfigResumenDto
                {
                    Grupo = f.Grupo, Nombre = f.Nombre, ReporteId = f.ReporteId, FamiliaCodigo = f.FamiliaCodigo, Orden = f.Orden
                }).ToList(), true);
            return (Sugerida(configs), false);
        }

        private static readonly (string Grupo, string[] Claves)[] Grupos =
        {
            ("Maquinaria Agrícola", new[] { "ALZADORA", "COSECHADORA", "FURGON", "TRACTOR" }),
            ("Maquinaria Automotriz", new[] { "AUTOVOLTEO", "BUS", "CABEZAL", "CAMION", "DOLLY", "LOW BOY", "LOWBOY", "GONDOLA", "JAULA" }),
            ("Maquinaria Ferroviaria", new[] { "LOCOMOTORA", "VAGON" }),
        };

        /// <summary>
        /// Configuración sugerida, como la lámina del Taller: las familias del programa principal en
        /// Agrícola / Automotriz / Ferroviaria, y los tractores separados por sus programas
        /// (Varios, Cosecha, Producción) en vez de la familia "Tractores" del principal.
        /// </summary>
        public static List<FilaConfigResumenDto> Sugerida(List<ConfigAvanceDto> configs)
        {
            var filas = new List<FilaConfigResumenDto>();
            var principal = configs.OrderByDescending(c => c.Familias.Count).FirstOrDefault();
            if (principal == null) return filas;

            string? GrupoDe(string familia) =>
                Grupos.FirstOrDefault(g => g.Claves.Any(k => Normalizar(familia).Contains(k))).Grupo;

            foreach (var (grupo, _) in Grupos)
            {
                foreach (var f in principal.Familias.OrderBy(x => x.Orden).Where(x => GrupoDe(x.FamiliaNombre) == grupo))
                {
                    if (Normalizar(f.FamiliaNombre).Contains("TRACTOR"))
                    {
                        // Tractores por programa, en el orden de la lámina.
                        foreach (var (clave, nombre) in new[] { ("VARIOS", "Tractores varios"), ("COSECHA", "Tractores de Cosecha"), ("PRODUCCION", "Tractores de Producción") })
                        {
                            var programa = configs.FirstOrDefault(c => c.ReporteId != principal.ReporteId && Normalizar(c.Nombre).Contains(clave));
                            var tractores = programa?.Familias.FirstOrDefault(x => Normalizar(x.FamiliaNombre).Contains("TRACTOR"));
                            if (programa != null && tractores != null)
                                filas.Add(new FilaConfigResumenDto { Grupo = grupo, Nombre = nombre, ReporteId = programa.ReporteId, FamiliaCodigo = tractores.FamiliaCodigo });
                        }
                        continue;
                    }
                    filas.Add(new FilaConfigResumenDto { Grupo = grupo, Nombre = f.FamiliaNombre, ReporteId = principal.ReporteId, FamiliaCodigo = f.FamiliaCodigo });
                }
            }
            for (var i = 0; i < filas.Count; i++) filas[i].Orden = i + 1;
            return filas;
        }

        private static string Normalizar(string texto)
        {
            var sinTildes = new string(texto.Normalize(NormalizationForm.FormD)
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
            return sinTildes.ToUpperInvariant();
        }
    }
}
