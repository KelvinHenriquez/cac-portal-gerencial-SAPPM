using PortalGerencialBackEnd.Core.Dtos.Sap;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;

namespace PortalGerencialBackEnd.Infrastructure.Integraciones.Local
{
    /// <summary>
    /// DataSourceMode=Local (sección 9): "SAP" sin SAP. Arma el universo de SAP con
    /// <see cref="SimuladorSap"/> a partir de la estructura de Oracle (local) y lo filtra por fecha
    /// de modificación igual que lo harían los módulos RFC del Anexo A v2.
    /// </summary>
    public class SapLocalSeedClient : ISapExtractionClient
    {
        private readonly IStructureSource _structureSource;
        private readonly TimeProvider _timeProvider;
        private SimuladorSap.Datos? _datos;

        public SapLocalSeedClient(IStructureSource structureSource, TimeProvider timeProvider)
        {
            _structureSource = structureSource;
            _timeProvider = timeProvider;
        }

        private async Task<SimuladorSap.Datos> DatosAsync()
        {
            if (_datos != null)
                return _datos;

            var equipos = await _structureSource.GetEquiposFamiliaAsync();
            // La ventana de cada familia sale de las semanas de su plan que tienen actividades (las
            // fechas estimadas de la familia en Oracle no sirven: son de un día); sus equipos, de las
            // OT que tiene asignadas.
            var programas = (await _structureSource.GetConfigsAvanceAsync())
                .SelectMany(r => r.Familias.Select(f =>
                {
                    var conPlan = f.Semanas.Where(s => s.Actividad > 0 || s.Dias > 0).ToList();
                    return new SimuladorSap.ProgramaFamilia(
                        r.ReporteId,
                        f.FamiliaCodigo,
                        conPlan.Count > 0 ? conPlan.Min(s => s.FechaInicio) : f.FechaEstInicio,
                        conPlan.Count > 0 ? conPlan.Max(s => s.FechaFin) : f.FechaEstFin,
                        f.Ordenes.Count > 0 ? f.Ordenes.Select(o => o.CodigoLegado).ToHashSet(StringComparer.OrdinalIgnoreCase) : null);
                }));

            _datos = SimuladorSap.Generar(
                equipos.Select(e => new SimuladorSap.EquipoOracle(e.CodigoLegado, e.Descripcion, e.FamiliaCodigo, e.Activo)),
                programas,
                _timeProvider.GetLocalNow().DateTime);
            return _datos;
        }

        public async Task<List<EquipoSapDto>> GetEquiposAsync(CancellationToken ct = default) =>
            (await DatosAsync()).Equipos.ToList();

        public async Task<List<OrdenSapDto>> GetOrdenesModificadasDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            (await DatosAsync()).Ordenes.Where(o => o.FechaModificacion.Date >= desde.Date).ToList();

        public async Task<List<OperacionSapDto>> GetOperacionesModificadasDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            (await DatosAsync()).Operaciones.Where(o => o.FechaModificacion.Date >= desde.Date).ToList();

        public async Task<List<AvisoSapDto>> GetAvisosModificadosDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            (await DatosAsync()).Avisos.Where(a => a.FechaModificacion.Date >= desde.Date).ToList();

        public async Task<List<HistorialEstadoDto>> GetHistorialEstadosDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            (await DatosAsync()).Historial.Where(h => h.FechaHoraCambio.Date >= desde.Date).ToList();
    }
}
