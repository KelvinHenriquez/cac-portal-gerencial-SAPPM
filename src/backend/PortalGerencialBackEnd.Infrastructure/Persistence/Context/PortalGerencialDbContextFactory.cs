using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Context
{
    /// <summary>Design-time factory para que `dotnet ef migrations add` funcione sin levantar la API completa.</summary>
    public class PortalGerencialDbContextFactory : IDesignTimeDbContextFactory<PortalGerencialDbContext>
    {
        public PortalGerencialDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PortalGerencialDbContext>();
            optionsBuilder.UseOracle(
                "User Id=PGPM;Password=PgpmLocal2026;Data Source=localhost:1522/FREEPDB1;",
                o => o.MigrationsHistoryTable(PortalGerencialDbContext.TablaHistorialMigraciones)
                      .UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19));

            return new PortalGerencialDbContext(optionsBuilder.Options);
        }
    }
}
