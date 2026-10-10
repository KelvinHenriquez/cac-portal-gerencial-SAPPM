using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PortalGerencialBackEnd.Core.Constants;
using PortalGerencialBackEnd.Core.Enums;
using PortalGerencialBackEnd.Core.Dtos.Auth;
using PortalGerencialBackEnd.Core.Dtos.Sistema;
using PortalGerencialBackEnd.Core.Entities;
using PortalGerencialBackEnd.Core.Exceptions;
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
        private readonly string _modo;
        private readonly bool _jobHabilitado;

        public SistemaService(PortalGerencialDbContext db, CoordinadorCorridas coordinador, TimeProvider time, IConfiguration configuration)
        {
            _db = db;
            _coordinador = coordinador;
            _time = time;
            _modo = (configuration.GetValue<DataSourceMode?>("DataSourceMode") ?? DataSourceMode.Local).ToString().ToUpperInvariant();
            _jobHabilitado = configuration.GetValue("Job:Habilitado", true);
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

            // Sin FirstOrDefault/Take en la base: en Oracle 11g generan FETCH FIRST, que no existe.
            // Solo interesan los errores posteriores al último éxito, que son pocos.
            var desdeUltimoOk = ultimaOk ?? DateTime.MinValue;
            var ultimoError = (await _db.LogExtracciones.AsNoTracking()
                    .Where(l => l.Estado == EstadosLog.Error && l.Inicio > desdeUltimoOk)
                    .ToListAsync())
                .MaxBy(l => l.Inicio);

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
                UltimoErrorMensaje = ultimoError != null && (ultimaOk == null || ultimoError.Inicio > ultimaOk) ? ultimoError.ErrorMensaje : null,
                Modo = _modo,
                JobHabilitado = _jobHabilitado,
                ProximaCorrida = _jobHabilitado
                    ? ActualizacionProgramadaService.ProximaCorrida(ahora, parametros.Hora(ClavesParametro.HoraCorridaDiaria, new TimeSpan(5, 0, 0)))
                    : null
            };
        }

        public async Task<List<AlertaDto>> GetAlertasAsync(int cantidad) =>
            // Take en memoria (Oracle 11g): son pocas filas, una por día de problema como mucho.
            (await _db.AlertasEnviadas.AsNoTracking()
                .Select(a => new AlertaDto
                {
                    Tipo = a.Tipo,
                    Fecha = a.Fecha,
                    Destinatarios = a.Destinatarios,
                    Enviada = a.Enviada,
                    Error = a.Error
                })
                .ToListAsync())
            .OrderByDescending(a => a.Fecha)
            .Take(Math.Clamp(cantidad, 1, 100))
            .ToList();

        public async Task<List<CorridaDto>> GetCorridasAsync(int cantidad)
        {
            // Agrupa en la base (una fila por corrida, ~1 por día) y recorta en memoria: Take en la
            // base generaría FETCH FIRST, que Oracle 11g no tiene.
            var ids = (await _db.LogExtracciones.AsNoTracking()
                    .GroupBy(l => l.CorridaId)
                    .Select(g => new { CorridaId = g.Key, Inicio = g.Min(l => l.Inicio) })
                    .ToListAsync())
                .OrderByDescending(x => x.Inicio)
                .Take(Math.Clamp(cantidad, 1, 200))
                .Select(x => x.CorridaId)
                .ToList();

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
            var ordenados = pasos.OrderBy(p => p.Inicio).ToList();
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
    
        // ------------------------------------------------------------ parámetros

        public async Task<ParametrosDto> GetParametrosAsync(PermisosDto permisos)
        {
            var orden = CatalogoParametros.Definiciones.Keys.Select((k, i) => (k, i)).ToDictionary(x => x.k, x => x.i);
            var parametros = await _db.Parametros.AsNoTracking().ToListAsync();
            return new ParametrosDto
            {
                PuedeEditar = permisos.EditarParametros,
                Parametros = parametros
                    .OrderBy(p => orden.GetValueOrDefault(p.Clave, int.MaxValue)).ThenBy(p => p.Clave)
                    .Select(p => ADto(p, permisos))
                    .ToList()
            };
        }

        public async Task<ParametroDto> ActualizarParametroAsync(string clave, string valor, string usuario, PermisosDto permisos)
        {
            if (!permisos.EditarParametros)
                throw new ForbiddenException("Su usuario puede ver los parámetros pero no cambiarlos (hace falta el rol ROL_PORTAL_PARAMETROS).");
            if (CatalogoParametros.EsTecnico(clave) && !permisos.EditarParametrosTecnicos)
                throw new ForbiddenException("Los parámetros técnicos (SAP y horario de la actualización) solo los cambia TI.");
            // Sin SingleOrDefault en la base: en Oracle 11g EF lo traduce a FETCH FIRST, que no existe.
            var parametro = (await _db.Parametros.Where(p => p.Clave == clave).ToListAsync()).SingleOrDefault()
                            ?? throw new NotFoundException($"No existe el parámetro '{clave}'.");
            var (normalizado, error) = CatalogoParametros.Validar(clave, valor);
            if (error != null)
                throw new BadRequestException(error);

            parametro.Valor = normalizado!;
            parametro.ActualizadoEn = _time.GetLocalNow().DateTime;
            parametro.ActualizadoPor = usuario.ToUpperInvariant();
            await _db.SaveChangesAsync();
            return ADto(parametro, permisos);
        }

        private static ParametroDto ADto(Parametro p, PermisosDto permisos)
        {
            var d = CatalogoParametros.Definiciones.GetValueOrDefault(p.Clave);
            var tecnico = CatalogoParametros.EsTecnico(p.Clave);
            return new ParametroDto
            {
                Clave = p.Clave,
                Seccion = d?.Seccion ?? "Otros",
                Nombre = d?.Nombre ?? p.Clave,
                Descripcion = p.Descripcion,
                Valor = p.Valor,
                Opciones = d?.Opciones,
                ActualizadoEn = p.ActualizadoEn,
                ActualizadoPor = p.ActualizadoPor,
                Tecnico = tecnico,
                Editable = permisos.EditarParametros && (!tecnico || permisos.EditarParametrosTecnicos)
            };
        }
    }
}
