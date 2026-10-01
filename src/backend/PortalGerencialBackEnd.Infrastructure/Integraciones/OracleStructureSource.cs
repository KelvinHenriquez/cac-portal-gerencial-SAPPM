using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Core.Dtos.Estructura;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Integraciones
{
    /// <summary>
    /// Lee en vivo la estructura de Oracle Forms a través de las vistas VW_*_PGPM (sección 4.1).
    /// Misma clase en local y en producción.
    /// </summary>
    public class OracleStructureSource : IStructureSource
    {
        private readonly PortalGerencialDbContext _db;

        public OracleStructureSource(PortalGerencialDbContext db)
        {
            _db = db;
        }

        public Task<List<TipoReporteDto>> GetTiposReporteAsync() =>
            _db.VwUmbralesSemaforo
                .OrderBy(x => x.Descripcion)
                .Select(x => new TipoReporteDto { TipoReporte = x.TipoReporte, Descripcion = x.Descripcion })
                .ToListAsync();

        public Task<UmbralesDto?> GetUmbralesAsync(string tipoReporte) =>
            _db.VwUmbralesSemaforo
                .Where(x => x.TipoReporte == tipoReporte)
                .Select(x => new UmbralesDto
                {
                    TipoReporte = x.TipoReporte,
                    UmbralRojo = x.UmbralRojo,
                    UmbralNaranja = x.UmbralNaranja
                })
                .FirstOrDefaultAsync();

        public Task<List<ProgramaDto>> GetProgramasAsync() =>
            _db.VwEstructuraFamilias
                .GroupBy(x => x.AnioPrograma)
                .Select(g => new ProgramaDto
                {
                    AnioPrograma = g.Key,
                    FechaInicio = g.Min(x => x.FechaEstInicio),
                    FechaFin = g.Max(x => x.FechaEstFin),
                    CantFamilias = g.Count()
                })
                .OrderByDescending(x => x.AnioPrograma)
                .ToListAsync();

        public Task<List<FamiliaDto>> GetFamiliasAsync(string anioPrograma) =>
            _db.VwEstructuraFamilias
                .Where(x => x.AnioPrograma == anioPrograma)
                .OrderBy(x => x.FamiliaNombre)
                .Select(x => new FamiliaDto
                {
                    AnioPrograma = x.AnioPrograma,
                    FamiliaCodigo = x.FamiliaCodigo,
                    FamiliaNombre = x.FamiliaNombre,
                    FechaEstInicio = x.FechaEstInicio,
                    FechaEstFin = x.FechaEstFin,
                    CantEquipo = x.CantEquipo
                })
                .ToListAsync();

        public Task<List<CurvaSemanaDto>> GetCurvaPlaneadaAsync(string anioPrograma) =>
            _db.VwCurvaPlaneada
                .Where(x => x.AnioPrograma == anioPrograma)
                .OrderBy(x => x.FamiliaCodigo).ThenBy(x => x.NoSemana)
                .Select(x => new CurvaSemanaDto
                {
                    AnioPrograma = x.AnioPrograma,
                    FamiliaCodigo = x.FamiliaCodigo,
                    Mes = x.Mes,
                    NoSemana = x.NoSemana,
                    FechaInicioSemana = x.FechaInicioSemana,
                    FechaFinSemana = x.FechaFinSemana,
                    ActividadPct = x.ActividadPct,
                    Dias = x.Dias
                })
                .ToListAsync();

        public Task<List<EquipoRequeridoDto>> GetEquiposRequeridosAsync(string tipoReporte) =>
            _db.VwEquiposRequeridos
                .Where(x => x.TipoReporte == tipoReporte)
                .OrderBy(x => x.FamiliaCodigo).ThenBy(x => x.Asignacion)
                .Select(x => new EquipoRequeridoDto
                {
                    TipoReporte = x.TipoReporte,
                    FamiliaCodigo = x.FamiliaCodigo,
                    Asignacion = x.Asignacion,
                    CantAsignada = x.CantAsignada,
                    CantRequerida = x.CantRequerida
                })
                .ToListAsync();

        public async Task<List<EquipoFamiliaDto>> GetEquiposFamiliaAsync(string? familiaCodigo = null)
        {
            var query = _db.VwEquiposFamilia.AsQueryable();
            if (!string.IsNullOrWhiteSpace(familiaCodigo))
                query = query.Where(x => x.FamiliaCodigo == familiaCodigo);

            var filas = await query.OrderBy(x => x.CodigoLegado).ToListAsync();
            return filas.Select(x => new EquipoFamiliaDto
            {
                CodigoLegado = x.CodigoLegado.Trim(),
                Descripcion = x.Descripcion,
                FamiliaCodigo = x.FamiliaCodigo,
                Activo = string.Equals(x.Activo?.Trim(), "S", StringComparison.OrdinalIgnoreCase)
            }).ToList();
        }
    }
}
