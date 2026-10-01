using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortalGerencialBackEnd.Core.Enums;
using PortalGerencialBackEnd.Core.Interfaces.Common;
using PortalGerencialBackEnd.Core.Interfaces.Integraciones;
using PortalGerencialBackEnd.Core.Interfaces.Repositories;
using PortalGerencialBackEnd.Core.Interfaces.Services;
using PortalGerencialBackEnd.Core.Settings;
using PortalGerencialBackEnd.Infrastructure.Integraciones;
using PortalGerencialBackEnd.Infrastructure.Integraciones.Local;
using PortalGerencialBackEnd.Infrastructure.Integraciones.Produccion;
using PortalGerencialBackEnd.Infrastructure.Jobs;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;
using PortalGerencialBackEnd.Infrastructure.Persistence.Interceptors;
using PortalGerencialBackEnd.Infrastructure.Persistence.Repositories;
using PortalGerencialBackEnd.Infrastructure.Seguridad;
using PortalGerencialBackEnd.Infrastructure.Services;

namespace PortalGerencialBackEnd.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var oracleSettings = configuration.GetSection("Oracle").Get<OracleSettings>() ?? new OracleSettings();
            var compatibilidad = oracleSettings.VersionSql switch
            {
                21 => OracleSQLCompatibility.DatabaseVersion21,
                23 => OracleSQLCompatibility.DatabaseVersion23,
                _ => OracleSQLCompatibility.DatabaseVersion19
            };

            // Un solo motor en todos los ambientes (sección 9): Oracle. En local es Oracle Free en
            // Docker; en producción, el Oracle de CAC con el usuario técnico del portal.
            services.AddDbContext<PortalGerencialDbContext>(options =>
            {
                options.UseOracle(configuration.GetConnectionString("Oracle"), o => o
                    .MigrationsHistoryTable(PortalGerencialDbContext.TablaHistorialMigraciones)
                    .UseOracleSQLCompatibility(compatibilidad));

                if (!string.IsNullOrWhiteSpace(oracleSettings.Esquema))
                    options.AddInterceptors(new CurrentSchemaInterceptor(oracleSettings.Esquema));
            });

            services.AddHttpContextAccessor();
            services.AddMemoryCache();
            services.AddScoped<IUserContextService, UserContextService>();

            services.AddScoped<IRepositoryManager, RepositoryManager>();
            services.AddScoped<IServiceManager, ServiceManager>();

            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IOracleCredentialValidator, OracleCredentialValidator>();
            services.AddScoped<IStructureSource, OracleStructureSource>();

            // Sección 9: DataSourceMode solo decide de dónde salen los datos de SAP. Oracle está
            // en ambos modos.
            var mode = configuration.GetValue<DataSourceMode?>("DataSourceMode") ?? DataSourceMode.Local;
            if (mode == DataSourceMode.Local)
                services.AddScoped<ISapExtractionClient, SapLocalSeedClient>();
            else
                services.AddScoped<ISapExtractionClient, SapRfcExtractionClient>();

            // Job de extracción y recálculo (sección 6).
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<CoordinadorCorridas>();
            services.AddScoped<ISistemaService, SistemaService>();
            services.AddScoped<IProcesoActualizacionService, ProcesoActualizacionService>();
            services.AddHostedService<ActualizacionProgramadaService>();

            // Reportes (solo leen resultados calculados y staging).
            services.AddScoped<IReporteDisponibilidadService, ReporteDisponibilidadService>();
            services.AddScoped<IReporteAvanceService, ReporteAvanceService>();
            services.AddSingleton<LoginAttemptLimiter>();

            services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
            services.Configure<OracleSettings>(configuration.GetSection("Oracle"));
            services.Configure<LoginSettings>(configuration.GetSection("Login"));

            return services;
        }
    }
}
