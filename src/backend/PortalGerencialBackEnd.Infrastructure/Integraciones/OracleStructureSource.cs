using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Entities.Estructura;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Integraciones
{
    /// <summary>
    /// Lee en vivo la configuración de Oracle Forms a través de las vistas VW_*_PGPM. Misma clase en
    /// local y en producción. Las tablas de configuración son chicas: se leen completas y se arman
    /// en memoria (además evita Take/First en la base, que en Oracle 11g no se pueden traducir).
    /// </summary>
    public class OracleStructureSource : IStructureSource
    {
        private readonly PortalGerencialDbContext _db;

        public OracleStructureSource(PortalGerencialDbContext db)
        {
            _db = db;
        }

        private static bool Si(string? valor) => string.Equals(valor?.Trim(), "S", StringComparison.OrdinalIgnoreCase);

        // ------------------------------------------------------------ disponibilidad

        public async Task<List<ReporteResumenDto>> GetReportesDisponibilidadAsync() =>
            (await _db.VwDispReportes.AsNoTracking().ToListAsync())
                .Where(r => Si(r.Activo))
                .OrderBy(r => r.Orden)
                .Select(r => new ReporteResumenDto { ReporteId = r.ReporteId, Descripcion = r.Descripcion })
                .ToList();

        public async Task<ConfigDisponibilidadDto?> GetConfigDisponibilidadAsync(int reporteId) =>
            (await ArmarConfigsDisponibilidadAsync(reporteId)).FirstOrDefault();

        public async Task<List<ConfigDisponibilidadDto>> GetConfigsDisponibilidadAsync() =>
            (await ArmarConfigsDisponibilidadAsync(null)).Where(c => c.Activo).ToList();

        private async Task<List<ConfigDisponibilidadDto>> ArmarConfigsDisponibilidadAsync(int? reporteId)
        {
            var reportes = await _db.VwDispReportes.AsNoTracking()
                .Where(r => reporteId == null || r.ReporteId == reporteId).ToListAsync();
            var ids = reportes.Select(r => r.ReporteId).ToList();

            var semanas = (await _db.VwDispSemanas.AsNoTracking().Where(s => ids.Contains(s.ReporteId)).ToListAsync()).ToLookup(s => s.ReporteId);
            var horas = (await _db.VwDispHoras.AsNoTracking().Where(h => ids.Contains(h.ReporteId)).ToListAsync()).ToLookup(h => h.ReporteId);
            var asignaciones = (await _db.VwDispAsignaciones.AsNoTracking().Where(a => ids.Contains(a.ReporteId)).ToListAsync()).ToLookup(a => a.ReporteId);
            var equipos = (await _db.VwDispAsigEquipos.AsNoTracking().Where(e => ids.Contains(e.ReporteId)).ToListAsync())
                .ToLookup(e => (e.ReporteId, e.AsignacionId));

            return reportes.OrderBy(r => r.Orden).Select(r => new ConfigDisponibilidadDto
            {
                ReporteId = r.ReporteId,
                Orden = r.Orden,
                Descripcion = r.Descripcion,
                UmbralRojo = r.UmbralRojo,
                UmbralNaranja = r.UmbralNaranja,
                Activo = Si(r.Activo),
                Semanas = semanas[r.ReporteId].OrderBy(s => s.NoSemana).Select(s => new SemanaCalendarioDto
                {
                    NoSemana = s.NoSemana,
                    Mes = s.Mes,
                    FechaInicio = s.FechaInicio.Date,
                    FechaFin = s.FechaFin.Date
                }).ToList(),
                HorasActivas = horas[r.ReporteId].Where(h => Si(h.Activo)).Select(h => h.Hora).OrderBy(h => h).ToList(),
                Asignaciones = asignaciones[r.ReporteId].OrderBy(a => a.Orden).Select(a => new AsignacionDto
                {
                    AsignacionId = a.AsignacionId,
                    Orden = a.Orden,
                    Nombre = a.Asignacion,
                    CantAsignada = a.CantAsignada,
                    CantRequerida = a.CantRequerida,
                    DispCompPct = a.DispCompPct,
                    Negrita = Si(a.Negrita),
                    Grupo = Si(a.Grupo),
                    Equipos = equipos[(r.ReporteId, a.AsignacionId)].Select(e => e.CodigoLegado.Trim()).OrderBy(e => e).ToList()
                }).ToList()
            }).ToList();
        }

        // ------------------------------------------------------------ avance

        public async Task<List<PeriodoDto>> GetPeriodosAsync()
        {
            var periodos = await _db.VwAvancePeriodos.AsNoTracking().ToListAsync();
            var reportes = (await _db.VwAvanceReportes.AsNoTracking().ToListAsync()).ToLookup(r => r.PeriodoId);
            return periodos
                .OrderByDescending(p => p.PeriodoId)
                .Select(p => new PeriodoDto
                {
                    PeriodoId = p.PeriodoId,
                    Descripcion = p.Descripcion,
                    Reportes = reportes[p.PeriodoId].OrderBy(r => r.ReporteId)
                        .Select(r => new ReporteResumenDto { ReporteId = r.ReporteId, Descripcion = r.Nombre }).ToList()
                })
                .ToList();
        }

        public async Task<ConfigAvanceDto?> GetConfigAvanceAsync(int reporteId) =>
            (await ArmarConfigsAvanceAsync(reporteId)).FirstOrDefault();

        public Task<List<ConfigAvanceDto>> GetConfigsAvanceAsync() => ArmarConfigsAvanceAsync(null);

        private async Task<List<ConfigAvanceDto>> ArmarConfigsAvanceAsync(int? reporteId)
        {
            var reportes = await _db.VwAvanceReportes.AsNoTracking()
                .Where(r => reporteId == null || r.ReporteId == reporteId).ToListAsync();
            var ids = reportes.Select(r => r.ReporteId).ToList();
            var periodos = (await _db.VwAvancePeriodos.AsNoTracking().ToListAsync()).ToDictionary(p => p.PeriodoId, p => p.Descripcion);
            var familias = (await _db.VwAvanceFamilias.AsNoTracking().Where(f => ids.Contains(f.ReporteId)).ToListAsync()).ToLookup(f => f.ReporteId);
            var semanas = (await _db.VwAvanceSemanas.AsNoTracking().Where(s => ids.Contains(s.ReporteId)).ToListAsync())
                .ToLookup(s => (s.ReporteId, s.FamiliaCodigo));
            var ordenes = (await _db.VwAvanceOts.AsNoTracking().Where(o => ids.Contains(o.ReporteId)).ToListAsync())
                .ToLookup(o => (o.ReporteId, o.FamiliaCodigo));

            return reportes.OrderBy(r => r.ReporteId).Select(r => new ConfigAvanceDto
            {
                ReporteId = r.ReporteId,
                PeriodoId = r.PeriodoId,
                PeriodoDescripcion = periodos.GetValueOrDefault(r.PeriodoId, r.PeriodoId.ToString()),
                Nombre = r.Nombre,
                TipoOt = r.TipoOt,
                FechaRef = r.FechaRef,
                FechaInicio = r.FechaInicio.Date,
                FechaFin = r.FechaFin.Date,
                Familias = familias[r.ReporteId].OrderBy(f => f.Orden).Select(f => new FamiliaAvanceDto
                {
                    Orden = f.Orden,
                    FamiliaCodigo = f.FamiliaCodigo,
                    FamiliaNombre = f.FamiliaNombre,
                    FechaEstInicio = f.FechaEstInicio.Date,
                    FechaEstFin = f.FechaEstFin.Date,
                    CantEquipo = f.CantEquipo,
                    Semanas = semanas[(r.ReporteId, f.FamiliaCodigo)].OrderBy(s => s.NoSemana).Select(s => new SemanaPlanDto
                    {
                        NoSemana = s.NoSemana,
                        Mes = s.Mes,
                        FechaInicio = s.FechaInicio.Date,
                        FechaFin = s.FechaFin.Date,
                        Actividad = s.Actividad,
                        Dias = s.Dias
                    }).ToList(),
                    Ordenes = ordenes[(r.ReporteId, f.FamiliaCodigo)].OrderBy(o => o.NoOt).Select(o => new OtAsignadaDto
                    {
                        NoOt = o.NoOt,
                        CodigoLegado = o.CodigoLegado.Trim(),
                        CantActividad = o.CantActividad,
                        CantDias = o.CantDias,
                        SemanaFin = o.SemanaFin
                    }).ToList()
                }).ToList()
            }).ToList();
        }

        public async Task<List<EquipoFamiliaDto>> GetEquiposFamiliaAsync(string? familiaCodigo = null)
        {
            var query = _db.VwEquiposFamilia.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(familiaCodigo))
                query = query.Where(x => x.FamiliaCodigo == familiaCodigo);

            return (await query.ToListAsync())
                .OrderBy(x => x.CodigoLegado)
                .Select(x => new EquipoFamiliaDto
                {
                    CodigoLegado = x.CodigoLegado.Trim(),
                    EquipoCodigo = x.EquipoCodigo?.Trim(),
                    Descripcion = x.Descripcion,
                    FamiliaCodigo = x.FamiliaCodigo,
                    Activo = Si(x.Activo)
                })
                .ToList();
        }
    }
}
