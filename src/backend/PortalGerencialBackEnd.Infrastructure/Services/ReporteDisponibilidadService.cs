using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Dtos.Disponibilidad;
using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    /// <summary>
    /// Pantalla Disponibilidad de Maquinaria (sección 8.2). La tabla principal sale de los
    /// resultados ya calculados; los drill-down leen además el staging (órdenes), nunca SAP.
    /// </summary>
    public class ReporteDisponibilidadService : IReporteDisponibilidadService
    {
        private const int TamanoLote = 900;

        private readonly PortalGerencialDbContext _db;
        private readonly IStructureSource _estructura;
        private readonly TimeProvider _time;

        public ReporteDisponibilidadService(PortalGerencialDbContext db, IStructureSource estructura, TimeProvider time)
        {
            _db = db;
            _estructura = estructura;
            _time = time;
        }

        public async Task<ReporteDisponibilidadDto> GetDisponibilidadAsync(string tipoReporte, int semanas)
        {
            semanas = Math.Clamp(semanas, 1, 26);
            var (tipo, umbrales) = await TipoYUmbralesAsync(tipoReporte);
            var nombres = await NombresFamiliaAsync();

            var fechaCorte = await _db.CalcDisponibilidadDiaria.AsNoTracking()
                .Where(d => d.TipoReporte == tipoReporte)
                .MaxAsync(d => (DateTime?)d.Fecha);

            var referencia = fechaCorte ?? _time.GetLocalNow().Date;
            var lunesActual = ISOWeek.ToDateTime(ISOWeek.GetYear(referencia), ISOWeek.GetWeekOfYear(referencia), DayOfWeek.Monday);
            var columnas = Enumerable.Range(0, semanas)
                .Select(i => lunesActual.AddDays(-7 * (semanas - 1 - i)))
                .Select(lunes => new SemanaColumnaDto
                {
                    AnioIso = ISOWeek.GetYear(lunes),
                    SemanaIso = ISOWeek.GetWeekOfYear(lunes),
                    FechaInicio = lunes,
                    FechaFin = lunes.AddDays(6)
                })
                .ToList();

            var reporte = new ReporteDisponibilidadDto
            {
                TipoReporte = tipo.TipoReporte,
                Descripcion = tipo.Descripcion,
                UmbralRojo = umbrales.UmbralRojo,
                UmbralNaranja = umbrales.UmbralNaranja,
                FechaCorte = fechaCorte,
                Semanas = columnas
            };
            if (fechaCorte == null)
                return reporte;

            var actuales = await _db.CalcDisponibilidadDiaria.AsNoTracking()
                .Where(d => d.TipoReporte == tipoReporte && d.Fecha == fechaCorte)
                .ToListAsync();

            var primerLunes = columnas[0].FechaInicio;
            var semanales = (await _db.CalcDisponibilidadSemanal.AsNoTracking()
                    .Where(s => s.TipoReporte == tipoReporte && s.FechaInicioSemana >= primerLunes)
                    .ToListAsync())
                .ToDictionary(s => (s.FamiliaCodigo, s.AnioIso, s.SemanaIso));

            string Color(decimal pct) => Semaforo.Color(pct, umbrales.UmbralRojo, umbrales.UmbralNaranja);

            reporte.Familias = actuales
                .Select(a => new FilaDisponibilidadDto
                {
                    FamiliaCodigo = a.FamiliaCodigo,
                    FamiliaNombre = nombres.GetValueOrDefault(a.FamiliaCodigo, a.FamiliaCodigo),
                    CantEquipo = a.CantEquipo,
                    EqRequerido = a.EqRequerido,
                    EqDisponible = a.EqDisponible,
                    EqFueraServicio = a.EqFueraServicio,
                    DispRealPct = a.DispRealPct,
                    DispPotencialPct = a.DispPotencialPct,
                    Semaforo = Color(a.DispRealPct),
                    Semanas = columnas.Select(c => semanales.TryGetValue((a.FamiliaCodigo, c.AnioIso, c.SemanaIso), out var s)
                        ? new CeldaSemanaDto
                        {
                            AnioIso = c.AnioIso,
                            SemanaIso = c.SemanaIso,
                            DispRealPct = s.DispRealPct,
                            DispRealMinPct = s.DispRealMinPct,
                            EqDisponibleProm = s.EqDisponibleProm,
                            EqRequerido = s.EqRequerido,
                            DiasCalculados = s.DiasCalculados,
                            Semaforo = Color(s.DispRealPct)
                        }
                        : new CeldaSemanaDto { AnioIso = c.AnioIso, SemanaIso = c.SemanaIso }).ToList()
                })
                .OrderBy(f => f.FamiliaNombre)
                .ToList();

            if (reporte.Familias.Count > 0)
            {
                // Total disponible / total requerido (decisión del 30/09/2026), con el sobrante de
                // cada familia recortado: los equipos de más en una familia no tapan la falta en otra.
                var totalReal = CalculadoraDisponibilidad.PorcentajeTotal(
                    reporte.Familias.Select(f => ((decimal)f.EqDisponible, f.EqRequerido)));
                reporte.Promedio = new FilaDisponibilidadDto
                {
                    FamiliaCodigo = "TODAS",
                    FamiliaNombre = "Todas (Promedio)",
                    CantEquipo = reporte.Familias.Sum(f => f.CantEquipo),
                    EqRequerido = reporte.Familias.Sum(f => f.EqRequerido),
                    EqDisponible = reporte.Familias.Sum(f => f.EqDisponible),
                    EqFueraServicio = reporte.Familias.Sum(f => f.EqFueraServicio),
                    DispRealPct = totalReal,
                    DispPotencialPct = CalculadoraDisponibilidad.Porcentaje(
                        reporte.Familias.Sum(f => f.EqDisponible), reporte.Familias.Sum(f => f.CantEquipo)),
                    Semaforo = Color(totalReal),
                    Semanas = columnas.Select((c, i) =>
                    {
                        var celdas = reporte.Familias.Select(f => f.Semanas[i]).Where(s => s.EqDisponibleProm != null).ToList();
                        if (celdas.Count == 0)
                            return new CeldaSemanaDto { AnioIso = c.AnioIso, SemanaIso = c.SemanaIso };
                        var pct = CalculadoraDisponibilidad.PorcentajeTotal(celdas.Select(s => (s.EqDisponibleProm!.Value, s.EqRequerido)));
                        return new CeldaSemanaDto
                        {
                            AnioIso = c.AnioIso,
                            SemanaIso = c.SemanaIso,
                            DispRealPct = pct,
                            EqDisponibleProm = celdas.Sum(s => s.EqDisponibleProm),
                            EqRequerido = celdas.Sum(s => s.EqRequerido),
                            DiasCalculados = celdas.Max(s => s.DiasCalculados),
                            Semaforo = Color(pct)
                        };
                    }).ToList()
                };
            }

            return reporte;
        }

        public async Task<EquiposFamiliaDetalleDto> GetEquiposFamiliaAsync(string tipoReporte, string familiaCodigo)
        {
            await TipoYUmbralesAsync(tipoReporte);
            var nombres = await NombresFamiliaAsync();
            var fecha = await _db.CalcDisponibilidadDiaria.AsNoTracking()
                            .Where(d => d.TipoReporte == tipoReporte && d.FamiliaCodigo == familiaCodigo)
                            .MaxAsync(d => (DateTime?)d.Fecha)
                        ?? _time.GetLocalNow().Date;

            var equipos = await _estructura.GetEquiposFamiliaAsync(familiaCodigo);
            if (equipos.Count == 0)
                throw new NotFoundException($"La familia '{familiaCodigo}' no tiene equipos en Oracle.");

            var (sapPorLegado, ordenesPorLegado) = await OrdenesDeEquiposAsync(equipos, fecha, fecha);
            var estadosFs = await EstadosFueraServicioAsync();

            var requeridos = (await _estructura.GetEquiposRequeridosAsync(tipoReporte))
                .Where(r => r.FamiliaCodigo == familiaCodigo)
                .Select(r => new RequeridoAsignacionDto { Asignacion = r.Asignacion, CantAsignada = r.CantAsignada, CantRequerida = r.CantRequerida })
                .ToList();

            var detalle = equipos.Select(e =>
            {
                var ordenes = ordenesPorLegado.GetValueOrDefault(e.CodigoLegado, new())
                    .Where(o => CubreDia(o, fecha, estadosFs))
                    .ToList();
                var enSap = sapPorLegado.ContainsKey(e.CodigoLegado);
                return new EquipoDetalleDto
                {
                    CodigoLegado = e.CodigoLegado,
                    Descripcion = e.Descripcion,
                    EquipoSapId = enSap ? sapPorLegado[e.CodigoLegado] : null,
                    Activo = e.Activo,
                    Estado = !e.Activo ? "INACTIVO"
                        : ordenes.Count > 0 ? "FUERA_SERVICIO"
                        : !enSap ? "SIN_SAP"
                        : "DISPONIBLE",
                    Ordenes = ordenes
                };
            })
            // Primero lo que requiere atención: fuera de servicio, luego sin SAP.
            .OrderBy(e => e.Estado switch { "FUERA_SERVICIO" => 0, "SIN_SAP" => 1, "DISPONIBLE" => 2, _ => 3 })
            .ThenBy(e => e.CodigoLegado)
            .ToList();

            return new EquiposFamiliaDetalleDto
            {
                FamiliaCodigo = familiaCodigo,
                FamiliaNombre = nombres.GetValueOrDefault(familiaCodigo, familiaCodigo),
                Fecha = fecha,
                Requeridos = requeridos,
                Equipos = detalle
            };
        }

        public async Task<DetalleSemanaDto> GetDetalleSemanaAsync(string tipoReporte, string familiaCodigo, int anioIso, int semanaIso)
        {
            if (semanaIso < 1 || semanaIso > ISOWeek.GetWeeksInYear(anioIso))
                throw new BadRequestException($"La semana {semanaIso} no existe en {anioIso}.");

            var (_, umbrales) = await TipoYUmbralesAsync(tipoReporte);
            var nombres = await NombresFamiliaAsync();
            var lunes = ISOWeek.ToDateTime(anioIso, semanaIso, DayOfWeek.Monday);
            var domingo = lunes.AddDays(6);

            var dias = await _db.CalcDisponibilidadDiaria.AsNoTracking()
                .Where(d => d.TipoReporte == tipoReporte && d.FamiliaCodigo == familiaCodigo && d.Fecha >= lunes && d.Fecha <= domingo)
                .OrderBy(d => d.Fecha)
                .ToListAsync();

            var equipos = (await _estructura.GetEquiposFamiliaAsync(familiaCodigo)).Where(e => e.Activo).ToList();
            var (_, ordenesPorLegado) = await OrdenesDeEquiposAsync(equipos, lunes, domingo);
            var estadosFs = await EstadosFueraServicioAsync();
            var todas = ordenesPorLegado.Values.SelectMany(o => o).ToList();

            return new DetalleSemanaDto
            {
                TipoReporte = tipoReporte,
                FamiliaCodigo = familiaCodigo,
                FamiliaNombre = nombres.GetValueOrDefault(familiaCodigo, familiaCodigo),
                AnioIso = anioIso,
                SemanaIso = semanaIso,
                FechaInicio = lunes,
                FechaFin = domingo,
                Dias = dias.Select(d => new DiaDisponibilidadDto
                {
                    Fecha = d.Fecha,
                    CantEquipo = d.CantEquipo,
                    EqRequerido = d.EqRequerido,
                    EqFueraServicio = d.EqFueraServicio,
                    EqDisponible = d.EqDisponible,
                    DispRealPct = d.DispRealPct,
                    Semaforo = Semaforo.Color(d.DispRealPct, umbrales.UmbralRojo, umbrales.UmbralNaranja),
                    Ordenes = todas.Where(o => CubreDia(o, d.Fecha, estadosFs))
                        .OrderBy(o => o.CodigoLegado).ThenBy(o => o.OrdenSapId)
                        .ToList()
                }).ToList()
            };
        }

        // ---------------------------------------------------------------- apoyo

        private async Task<(TipoReporteDto Tipo, UmbralesDto Umbrales)> TipoYUmbralesAsync(string tipoReporte)
        {
            var tipo = (await _estructura.GetTiposReporteAsync()).FirstOrDefault(t => t.TipoReporte == tipoReporte)
                       ?? throw new NotFoundException($"No existe el tipo de reporte '{tipoReporte}'.");
            var umbrales = (await _estructura.GetUmbralesAsync(tipoReporte))!;
            return (tipo, umbrales);
        }

        /// <summary>Código de familia → nombre, del programa más reciente en que aparece.</summary>
        private async Task<Dictionary<string, string>> NombresFamiliaAsync()
        {
            var nombres = new Dictionary<string, string>();
            foreach (var programa in (await _estructura.GetProgramasAsync()).OrderBy(p => p.AnioPrograma))
            {
                foreach (var f in await _estructura.GetFamiliasAsync(programa.AnioPrograma))
                    nombres[f.FamiliaCodigo] = f.FamiliaNombre;
            }
            return nombres;
        }

        private async Task<HashSet<string>> EstadosFueraServicioAsync() =>
            (await ParametrosPortal.CargarAsync(_db)).Lista(ClavesParametro.EstadosFueraServicio, "Liberado,En Proceso");

        /// <summary>Misma regla que el cálculo del job (CalculadoraDisponibilidad.VentanaFueraServicio).</summary>
        private bool CubreDia(OrdenResumenDto o, DateTime dia, ISet<string> estadosFs)
        {
            var ventana = CalculadoraDisponibilidad.VentanaFueraServicio(
                new OrdenDisponibilidad(o.CodigoLegado, o.EstadoOrden, o.FechaInicioReal, o.FechaFinReal),
                estadosFs, _time.GetLocalNow().Date);
            return ventana is { } v && v.Desde <= dia.Date && dia.Date <= v.Hasta;
        }

        /// <summary>
        /// Equipo SAP de cada código legado, y las órdenes del staging de esos equipos que pueden
        /// cubrir algún día del rango (empezaron antes del fin y no terminaron antes del inicio).
        /// </summary>
        private async Task<(Dictionary<string, string> SapPorLegado, Dictionary<string, List<OrdenResumenDto>> OrdenesPorLegado)>
            OrdenesDeEquiposAsync(IEnumerable<EquipoFamiliaDto> equipos, DateTime desde, DateTime hasta)
        {
            var legados = equipos.Select(e => e.CodigoLegado).ToList();
            var descripciones = equipos.ToDictionary(e => e.CodigoLegado, e => e.Descripcion, StringComparer.OrdinalIgnoreCase);

            var sapPorLegado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var legadoPorSap = new Dictionary<string, string>();
            foreach (var lote in legados.Chunk(TamanoLote))
            {
                var filas = await _db.StgSapEquipos.AsNoTracking()
                    .Where(e => e.CodigoLegado != null && lote.Contains(e.CodigoLegado))
                    .Select(e => new { e.EquipoSapId, e.CodigoLegado })
                    .ToListAsync();
                foreach (var f in filas)
                {
                    sapPorLegado.TryAdd(f.CodigoLegado!, f.EquipoSapId);
                    legadoPorSap[f.EquipoSapId] = f.CodigoLegado!;
                }
            }

            var ordenes = new Dictionary<string, List<OrdenResumenDto>>(StringComparer.OrdinalIgnoreCase);
            foreach (var lote in legadoPorSap.Keys.Chunk(TamanoLote))
            {
                var filas = await _db.StgSapOrdenes.AsNoTracking()
                    .Where(o => o.EquipoSapId != null && lote.Contains(o.EquipoSapId)
                                && o.FechaInicioReal != null && o.FechaInicioReal <= hasta
                                && (o.FechaFinReal == null || o.FechaFinReal >= desde))
                    .ToListAsync();

                foreach (var o in filas)
                {
                    var legado = legadoPorSap[o.EquipoSapId!];
                    if (!ordenes.TryGetValue(legado, out var lista))
                        ordenes[legado] = lista = new List<OrdenResumenDto>();
                    lista.Add(new OrdenResumenDto
                    {
                        OrdenSapId = o.OrdenSapId,
                        ClaseOrden = o.ClaseOrden,
                        EstadoOrden = o.EstadoOrden,
                        Descripcion = o.Descripcion,
                        CodigoLegado = legado,
                        EquipoDescripcion = descripciones.GetValueOrDefault(legado),
                        FechaInicioReal = o.FechaInicioReal,
                        FechaFinReal = o.FechaFinReal
                    });
                }
            }

            return (sapPorLegado, ordenes);
        }
    }
}
