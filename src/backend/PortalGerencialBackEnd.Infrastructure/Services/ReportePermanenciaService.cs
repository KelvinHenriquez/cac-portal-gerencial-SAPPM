using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Core.Calculo;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Dtos.Permanencia;
using PortalGerencialBackEnd.Core.Entities.Calculo;
using PortalGerencialBackEnd.Core.Exceptions;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    /// <summary>
    /// Tiempo de permanencia por área → familia (tipo de equipo SAP) → equipo → orden. Los
    /// promedios usan solo las órdenes TERMINADAS cuya fecha de finalizada cae en el período; las
    /// abiertas se cuentan aparte, con su antigüedad (decisión del 06/10/2026).
    /// </summary>
    public class ReportePermanenciaService : IReportePermanenciaService
    {
        /// <summary>Período por defecto: los últimos 3 meses hasta hoy.</summary>
        private const int MesesPorDefecto = 3;

        private readonly PortalGerencialDbContext _db;
        private readonly TimeProvider _time;

        public ReportePermanenciaService(PortalGerencialDbContext db, TimeProvider time)
        {
            _db = db;
            _time = time;
        }

        public async Task<List<AreaPermanenciaDto>> GetAreasAsync() =>
            ReglasPortal.AreasPermanencia(await ParametrosPortal.CargarAsync(_db))
                .Select(a => new AreaPermanenciaDto { Codigo = a.Codigo, Nombre = a.Nombre })
                .ToList();

        public async Task<ResumenPermanenciaDto> GetResumenAsync(string area, DateTime? desde, DateTime? hasta)
        {
            var parametros = await ParametrosPortal.CargarAsync(_db);
            var nombreArea = ReglasPortal.AreasPermanencia(parametros)
                .FirstOrDefault(a => a.Codigo.Equals(area, StringComparison.OrdinalIgnoreCase)).Nombre
                ?? throw new NotFoundException($"El área '{area}' no está configurada (PERMANENCIA.AREAS).");
            var (d, h) = Periodo(desde, hasta);
            var filas = await FilasAsync(q => q.Where(p => p.Area == area.ToUpper()));

            return new ResumenPermanenciaDto
            {
                Area = area.ToUpperInvariant(),
                AreaNombre = nombreArea,
                Desde = d,
                Hasta = h,
                FechaCalculo = filas.Count == 0 ? null : filas.Max(f => f.GeneradoEn),
                EsperaRepuestosConfigurada = ReglasPortal.EstadosPermanencia(parametros).EsperaRepuestos != null,
                Total = Indicador(area.ToUpperInvariant(), nombreArea, filas, d, h),
                Familias = filas.GroupBy(f => f.TipoEquipo)
                    .Select(g => Indicador(g.Key, g.First().TipoEquipoDesc ?? g.Key, g.ToList(), d, h))
                    .Where(i => i.Ordenes + i.OrdenesEnCurso > 0)
                    .OrderByDescending(i => i.PromedioHoras).ThenBy(i => i.Nombre)
                    .ToList()
            };
        }

        public async Task<FamiliaPermanenciaDto> GetFamiliaAsync(string area, string tipoEquipo, DateTime? desde, DateTime? hasta)
        {
            var (d, h) = Periodo(desde, hasta);
            var filas = await FilasAsync(q => q.Where(p => p.Area == area.ToUpper() && p.TipoEquipo == tipoEquipo));
            if (filas.Count == 0)
                throw new NotFoundException($"No hay órdenes correctivas del tipo de equipo '{tipoEquipo}' en el área {area}.");
            return new FamiliaPermanenciaDto
            {
                Familia = Indicador(tipoEquipo, filas[0].TipoEquipoDesc ?? tipoEquipo, filas, d, h),
                Equipos = filas.GroupBy(f => f.EquipoSapId)
                    .Select(g =>
                    {
                        var i = Indicador(g.Key, g.First().EquipoDescripcion ?? g.Key, g.ToList(), d, h);
                        i.Clave = g.First().ClaveEquipo;
                        return i;
                    })
                    .Where(i => i.Ordenes + i.OrdenesEnCurso > 0)
                    .OrderByDescending(i => i.PromedioHoras).ThenBy(i => i.Nombre)
                    .ToList()
            };
        }

        public async Task<EquipoPermanenciaDto> GetEquipoAsync(string equipoSapId, DateTime? desde, DateTime? hasta, IReadOnlyCollection<string> areas)
        {
            var (d, h) = Periodo(desde, hasta);
            var filas = (await FilasAsync(q => q.Where(p => p.EquipoSapId == equipoSapId)))
                .Where(f => areas.Contains(f.Area, StringComparer.OrdinalIgnoreCase))
                .ToList();
            if (filas.Count == 0)
                throw new NotFoundException($"El equipo '{equipoSapId}' no tiene órdenes correctivas.");
            var equipo = Indicador(equipoSapId, filas[0].EquipoDescripcion ?? equipoSapId, filas, d, h);
            equipo.Clave = filas[0].ClaveEquipo;
            return new EquipoPermanenciaDto
            {
                Equipo = equipo,
                TipoEquipo = filas[0].TipoEquipo,
                TipoEquipoDesc = filas[0].TipoEquipoDesc,
                Ordenes = filas.Where(f => !f.Terminada || EnPeriodo(f, d, h))
                    .OrderByDescending(f => f.Inicio)
                    .Select(f => new OrdenPermanenciaDto
                    {
                        OrdenSapId = f.OrdenSapId,
                        ClaseOrden = f.ClaseOrden,
                        Descripcion = f.Descripcion,
                        AvisoSapId = f.AvisoSapId,
                        Terminada = f.Terminada,
                        Inicio = f.Inicio,
                        FechaAviso = f.FechaAviso,
                        FechaCreada = f.FechaCreada,
                        FechaLiberada = f.FechaLiberada,
                        FechaEnProceso = f.FechaEnProceso,
                        FechaFinalizada = f.FechaFinalizada,
                        FechaCierreTecnico = f.FechaCierreTecnico,
                        HorasPermanencia = f.HorasPermanencia,
                        Pasos = new PasosPermanenciaDto
                        {
                            Respuesta = f.HorasRespuesta,
                            Liberacion = f.HorasLiberacion,
                            EsperaInicio = f.HorasEsperaInicio,
                            Ejecucion = f.HorasEjecucion,
                            CierreAdministrativo = f.HorasCierreAdm,
                            EsperaRepuestos = f.HorasEsperaRepuestos
                        }
                    })
                    .ToList()
            };
        }

        // ------------------------------------------------------------ apoyo

        private async Task<List<CalcPermanencia>> FilasAsync(Func<IQueryable<CalcPermanencia>, IQueryable<CalcPermanencia>> filtro) =>
            await filtro(_db.CalcPermanencias.AsNoTracking()).ToListAsync();

        private (DateTime Desde, DateTime Hasta) Periodo(DateTime? desde, DateTime? hasta)
        {
            var hoy = _time.GetLocalNow().DateTime.Date;
            var h = (hasta ?? hoy).Date;
            var d = (desde ?? h.AddMonths(-MesesPorDefecto).AddDays(1)).Date;
            if (d > h)
                throw new BadRequestException("La fecha desde no puede ser posterior a la fecha hasta.");
            return (d, h);
        }

        private static bool EnPeriodo(CalcPermanencia f, DateTime desde, DateTime hasta) =>
            f.Terminada && f.FechaFinalizada is { } fin && fin.Date >= desde && fin.Date <= hasta;

        private static IndicadorPermanenciaDto Indicador(string codigo, string nombre, IReadOnlyCollection<CalcPermanencia> filas, DateTime desde, DateTime hasta)
        {
            var terminadas = filas.Where(f => EnPeriodo(f, desde, hasta)).ToList();
            var abiertas = filas.Where(f => !f.Terminada).ToList();
            var (promedio, mediana) = CalculadoraPermanencia.Estadisticos(terminadas.Select(f => f.HorasPermanencia).ToList());

            static decimal? Prom(IEnumerable<decimal?> valores)
            {
                var v = valores.OfType<decimal>().ToList();
                return v.Count == 0 ? null : Math.Round(v.Average(), 2);
            }

            return new IndicadorPermanenciaDto
            {
                Codigo = codigo,
                Nombre = nombre,
                Ordenes = terminadas.Count,
                Equipos = terminadas.Select(f => f.EquipoSapId).Distinct().Count(),
                PromedioHoras = promedio,
                MedianaHoras = mediana,
                MaximoHoras = terminadas.Count == 0 ? 0 : terminadas.Max(f => f.HorasPermanencia),
                Pasos = new PasosPermanenciaDto
                {
                    Respuesta = Prom(terminadas.Select(f => f.HorasRespuesta)),
                    Liberacion = Prom(terminadas.Select(f => f.HorasLiberacion)),
                    EsperaInicio = Prom(terminadas.Select(f => f.HorasEsperaInicio)),
                    Ejecucion = Prom(terminadas.Select(f => f.HorasEjecucion)),
                    CierreAdministrativo = Prom(terminadas.Select(f => f.HorasCierreAdm)),
                    EsperaRepuestos = Prom(terminadas.Select(f => (decimal?)f.HorasEsperaRepuestos))
                },
                OrdenesEnCurso = abiertas.Count,
                AntiguedadEnCursoHoras = abiertas.Count == 0 ? null : Math.Round(abiertas.Average(f => f.HorasPermanencia), 2)
            };
        }
    }
}
