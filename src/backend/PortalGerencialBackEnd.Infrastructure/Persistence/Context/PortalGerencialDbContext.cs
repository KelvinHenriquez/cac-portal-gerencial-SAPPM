using Microsoft.EntityFrameworkCore;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Context
{
    /// <summary>
    /// Tablas propias del portal en Oracle (sección 3): staging de SAP, resultados calculados y
    /// bitácora — todas con sufijo _pgpm. Las entidades se agregan en la Etapa 2.
    /// </summary>
    public class PortalGerencialDbContext : DbContext
    {
        /// <summary>
        /// Historial de migraciones de EF. Lleva el sufijo _pgpm como todo objeto que crea el
        /// portal, para no chocar con nada existente en el esquema Oracle.
        /// </summary>
        public const string TablaHistorialMigraciones = "EF_MIGRACIONES_PGPM";

        public PortalGerencialDbContext(DbContextOptions<PortalGerencialDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortalGerencialDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
