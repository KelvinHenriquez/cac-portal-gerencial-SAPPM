using PortalGerencialBackEnd.Core.Dtos.Sap;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;

namespace PortalGerencialBackEnd.Infrastructure.Integraciones.Local
{
    /// <summary>
    /// DataSourceMode=Local (sección 9): "SAP" sin SAP. Arma el universo de SAP con
    /// <see cref="SimuladorSap"/> a partir de la estructura de Oracle (local) y lo filtra por
    /// fecha de modificación igual que lo harían los módulos RFC del Anexo A.
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
            var familias = new List<SimuladorSap.FamiliaPrograma>();
            foreach (var programa in await _structureSource.GetProgramasAsync())
            {
                familias.AddRange((await _structureSource.GetFamiliasAsync(programa.AnioPrograma))
                    .Select(f => new SimuladorSap.FamiliaPrograma(f.FamiliaCodigo, f.FechaEstInicio, f.FechaEstFin)));
            }

            _datos = SimuladorSap.Generar(
                equipos.Select(e => new SimuladorSap.EquipoOracle(e.CodigoLegado, e.Descripcion, e.FamiliaCodigo, e.Activo)),
                familias,
                _timeProvider.GetLocalNow().DateTime);
            return _datos;
        }

        public async Task<List<EquipoSapDto>> GetEquiposAsync(CancellationToken ct = default) =>
            (await DatosAsync()).Equipos.ToList();

        public async Task<List<OrdenSapDto>> GetOrdenesModificadasDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            (await DatosAsync()).Ordenes.Where(o => o.FechaModificacion.Date >= desde.Date).ToList();

        public async Task<List<HistorialEstadoDto>> GetHistorialEstadosDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            (await DatosAsync()).Historial.Where(h => h.FechaHoraCambio.Date >= desde.Date).ToList();

        public async Task<List<OperacionHorasDto>> GetHorasModificadasDesdeAsync(DateTime desde, CancellationToken ct = default) =>
            (await DatosAsync()).Operaciones.Where(o => o.Modificacion.Date >= desde.Date).Select(o => o.Operacion).ToList();
    }
}
