using PortalGerencialBackEnd.Core.Dtos.Auth;
using PortalGerencialBackEnd.Core.Dtos.ResumenAvance;

namespace PortalGerencialBackEnd.Core.Interfaces.Services
{
    public interface IResumenAvanceService
    {
        /// <param name="semanaFin">Último día de la semana a mostrar; null = la semana actual.</param>
        Task<ResumenAvanceDto> GetResumenAsync(string area, DateTime? semanaFin, string medida, PermisosDto permisos);
        Task<ConfigResumenDto> GetConfigAsync(string area);
        Task<ConfigResumenDto> GuardarConfigAsync(string area, List<FilaConfigResumenDto> filas, string usuario, PermisosDto permisos);
        Task GuardarNotaAsync(string area, GuardarNotaResumenRequest nota, string usuario, PermisosDto permisos);
    }
}
