using Microsoft.Extensions.Diagnostics.HealthChecks;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

namespace PortalGerencialBackEnd.API.Salud
{
    /// <summary>
    /// /api/salud: la API responde y llega a Oracle con el usuario técnico. Lo usan el healthcheck de
    /// Docker y el monitoreo del servidor; no expone datos.
    /// </summary>
    public class OracleHealthCheck : IHealthCheck
    {
        private readonly PortalGerencialDbContext _db;

        public OracleHealthCheck(PortalGerencialDbContext db)
        {
            _db = db;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy("Oracle OK")
                    : HealthCheckResult.Unhealthy("Sin conexión a Oracle");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Sin conexión a Oracle", ex);
            }
        }
    }
}
