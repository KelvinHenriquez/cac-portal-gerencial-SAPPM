using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.Core.Entities;
using PortalGerencialBackEnd.Core.Entities.Calculo;
using PortalGerencialBackEnd.Core.Entities.Estructura;
using PortalGerencialBackEnd.Core.Entities.Sap;

namespace PortalGerencialBackEnd.Infrastructure.Persistence.Context
{
    /// <summary>
    /// Objetos del portal en Oracle (sección 3): staging de SAP, resultados calculados, bitácora y
    /// parámetros — todos con sufijo _pgpm — más las vistas de estructura de solo lectura.
    /// </summary>
    public class PortalGerencialDbContext : DbContext
    {
        /// <summary>
        /// Historial de migraciones de EF. Lleva el sufijo _pgpm como todo objeto que crea el
        /// portal, para no chocar con nada existente en el esquema Oracle.
        /// </summary>
        public const string TablaHistorialMigraciones = "EF_MIGRACIONES_PGPM";

        public PortalGerencialDbContext(DbContextOptions<PortalGerencialDbContext> options) : base(options) { }

        // Staging de SAP
        public DbSet<StgSapEquipo> StgSapEquipos => Set<StgSapEquipo>();
        public DbSet<StgSapOrden> StgSapOrdenes => Set<StgSapOrden>();
        public DbSet<StgSapOperacionHoras> StgSapOperacionesHoras => Set<StgSapOperacionHoras>();
        public DbSet<StgSapHistorialEstado> StgSapHistorialEstados => Set<StgSapHistorialEstado>();

        // Resultados calculados
        public DbSet<CalcDisponibilidadDiaria> CalcDisponibilidadDiaria => Set<CalcDisponibilidadDiaria>();
        public DbSet<CalcDisponibilidadSemanal> CalcDisponibilidadSemanal => Set<CalcDisponibilidadSemanal>();
        public DbSet<CalcAvanceSemanal> CalcAvanceSemanal => Set<CalcAvanceSemanal>();
        public DbSet<CalcEquipoSinClasificar> CalcEquiposSinClasificar => Set<CalcEquipoSinClasificar>();

        // Sistema
        public DbSet<LogExtraccion> LogExtracciones => Set<LogExtraccion>();
        public DbSet<Parametro> Parametros => Set<Parametro>();
        public DbSet<AlertaEnviada> AlertasEnviadas => Set<AlertaEnviada>();

        // Vistas de estructura (solo lectura)
        public DbSet<VwUmbralSemaforo> VwUmbralesSemaforo => Set<VwUmbralSemaforo>();
        public DbSet<VwEstructuraFamilia> VwEstructuraFamilias => Set<VwEstructuraFamilia>();
        public DbSet<VwCurvaPlaneada> VwCurvaPlaneada => Set<VwCurvaPlaneada>();
        public DbSet<VwEquipoRequerido> VwEquiposRequeridos => Set<VwEquipoRequerido>();
        public DbSet<VwEquipoFamilia> VwEquiposFamilia => Set<VwEquipoFamilia>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // VARCHAR2 en vez de NVARCHAR2, como el DDL de la spec y el resto del esquema de CAC.
            configurationBuilder.Properties<string>().AreUnicode(false);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortalGerencialDbContext).Assembly);
            base.OnModelCreating(modelBuilder);

            // Columnas en MAYÚSCULAS_CON_GUION (EquipoSapId -> EQUIPO_SAP_ID): EF entrecomilla los
            // identificadores, y en Oracle un nombre en minúsculas entre comillas obliga a todo el
            // que consulte la tabla a escribirlo igual. En mayúsculas se consultan sin comillas.
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                    property.SetColumnName(AMayusculasConGuion(property.Name));
            }
        }

        private static string AMayusculasConGuion(string nombre) =>
            Regex.Replace(nombre, "(?<=[a-z0-9])([A-Z])", "_$1").ToUpperInvariant();
    }
}
