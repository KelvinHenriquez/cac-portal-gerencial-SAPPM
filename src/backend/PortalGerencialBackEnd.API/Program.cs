using Microsoft.EntityFrameworkCore;
using PortalGerencialBackEnd.API.Extensions;
using PortalGerencialBackEnd.API.Middlewares;
using PortalGerencialBackEnd.API.Salud;
using PortalGerencialBackEnd.Infrastructure.Persistence.Context;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<OracleHealthCheck>("oracle");

var app = builder.Build();

app.UseForwardedHeaders();
app.UseCustomExceptionHandler();

// Aplica migraciones pendientes al arrancar (mismo patrón que TPresto). En producción puede
// apagarse con Database:AplicarMigracionesAlArrancar=false si el DBA prefiere correr él mismo el
// script (dotnet ef migrations script --idempotent) y dejar al usuario técnico sin permisos de DDL.
if (app.Configuration.GetValue("Database:AplicarMigracionesAlArrancar", true))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PortalGerencialDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error aplicando migraciones al arrancar.");
    }
}

app.UseDevelopmentConfiguration();

app.UseCors("LocalFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/salud");

app.Run();
