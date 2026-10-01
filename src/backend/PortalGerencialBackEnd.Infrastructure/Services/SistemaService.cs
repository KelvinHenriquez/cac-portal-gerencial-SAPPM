using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Dtos.Sistema;
using PortalGerencialBackEnd.Core.Entities;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Infrastructure.Jobs;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.Infrastructure.Services
{
    /// <summary>Lecturas de la bitácora y de los equipos sin clasificar (secciones 8.4, 10 y 11).</summary>
    public class SistemaService : ISistemaService
    {
        private readonly PortalGerencialDbContext _db;
        private readonly CoordinadorCorridas _coordinador;
        private readonly TimeProvider _time;

        public SistemaService(PortalGerencialDbContext db, CoordinadorCorridas coordinador, TimeProvider time)
        {
            _db = db;
            _coordinador = coordinador;
            _time = time;
        }

        public async Task<EstadoActualizacionDto> GetEstadoActualizacionAsync()
        {
            var parametros = await ParametrosPortal.CargarAsync(_db);
            var horasAlerta = parametros.Entero(ClavesParametro.HorasAlertaFrescura, 24);

            // El último paso de una corrida es el cálculo de avance, y la corrida se detiene en el
            // primer error: si ese paso terminó OK, la corrida entera terminó OK.
            var ultimaOk = await _db.LogExtracciones.AsNoTracking()
                .Where(l => l.Fuente == FuentesLog.CalcAvance && l.Estado == EstadosLog.Ok)
                .MaxAsync(l => l.Fin);

            var ultimoError = await _db.LogExtracciones.AsNoTracking()
                .Where(l => l.Estado == EstadosLog.Error)
                .OrderByDescending(l => l.Inicio)
                .FirstOrDefaultAsync();

            var ahora = _time.GetLocalNow().DateTime;
            double? horasDesde = ultimaOk == null ? null : Math.Round((ahora - ultimaOk.Value).TotalHours, 1);

            return new EstadoActualizacionDto
            {
                UltimaActualizacion = ultimaOk,
                HorasDesdeUltimaActualizacion = horasDesde,
                Desactualizado = horasDesde == null || horasDesde > horasAlerta,
                HorasAlerta = horasAlerta,
                CorridaEnCurso = _coordinador.EnCurso,
                // Solo interesa si el error es posterior al último éxito: si no, ya se recuperó.
                UltimoErrorFecha = ultimoError != null && (ultimaOk == null || ultimoError.Inicio > ultimaOk) ? ultimoError.Inicio : null,
                UltimoErrorMensaje = ultimoError != null && (ultimaOk == null || ultimoError.Inicio > ultimaOk) ? ultimoError.ErrorMensaje : null
            };
        }

        public async Task<List<CorridaDto>> GetCorridasAsync(int cantidad)
        {
            var ids = await _db.LogExtracciones.AsNoTracking()
                .GroupBy(l => l.CorridaId)
                .Select(g => new { CorridaId = g.Key, Inicio = g.Min(l => l.Inicio) })
                .OrderByDescending(x => x.Inicio)
                .Take(Math.Clamp(cantidad, 1, 200))
                .Select(x => x.CorridaId)
                .ToListAsync();

            var pasos = await _db.LogExtracciones.AsNoTracking()
                .Where(l => ids.Contains(l.CorridaId))
                .ToListAsync();

            return pasos
                .GroupBy(l => l.CorridaId)
                .Select(ACorrida)
                .OrderByDescending(c => c.Inicio)
                .ToList();
        }

        public async Task<CorridaDto?> GetCorridaAsync(string corridaId)
        {
            var pasos = await _db.LogExtracciones.AsNoTracking().Where(l => l.CorridaId == corridaId).ToListAsync();
            return pasos.Count == 0 ? null : ACorrida(pasos.GroupBy(l => l.CorridaId).Single());
        }

        public Task<List<EquipoSinClasificarDto>> GetEquiposSinClasificarAsync() =>
            _db.CalcEquiposSinClasificar.AsNoTracking()
                .OrderByDescending(e => e.CantOrdenes).ThenBy(e => e.EquipoSapId)
                .Select(e => new EquipoSinClasificarDto
                {
                    EquipoSapId = e.EquipoSapId,
                    CodigoLegado = e.CodigoLegado,
                    Descripcion = e.Descripcion,
                    PuestoTrabajo = e.PuestoTrabajo,
                    Motivo = e.Motivo,
                    CantOrdenes = e.CantOrdenes,
                    DetectadoEn = e.DetectadoEn
                })
                .ToListAsync();

        private static CorridaDto ACorrida(IGrouping<string, LogExtraccion> pasos)
        {
            var ordenados = pasos.OrderBy(p => p.Inicio).ThenBy(p => p.Id).ToList();
            var estado = ordenados.Any(p => p.Estado == EstadosLog.Error) ? EstadosLog.Error
                : ordenados.Any(p => p.Estado == EstadosLog.EnCurso) || ordenados.All(p => p.Fuente != FuentesLog.CalcAvance) ? EstadosLog.EnCurso
                : EstadosLog.Ok;

            return new CorridaDto
            {
                CorridaId = pasos.Key,
                Origen = ordenados[0].Origen,
                Inicio = ordenados[0].Inicio,
                Fin = estado == EstadosLog.EnCurso ? null : ordenados.Max(p => p.Fin),
                Estado = estado,
                Pasos = ordenados.Select(p => new PasoCorridaDto
                {
                    Fuente = p.Fuente,
                    Estado = p.Estado,
                    Inicio = p.Inicio,
                    Fin = p.Fin,
                    RegistrosProcesados = p.RegistrosProcesados,
                    FechaCorteDesde = p.FechaCorteDesde,
                    ErrorMensaje = p.ErrorMensaje
                }).ToList()
            };
        }
    }
}
