using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PortalGerencialBackEnd.Core.Dtos.Responses;
using PortalGerencialBackEnd.Infrastructure.DependencyInjection;

namespace PortalGerencialBackEnd.API.Extensions
{
    public static class ApplicationServicesExtensions
    {
        public const string PoliticaLogin = "login";

        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddInfrastructure(configuration);

            var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);
            const string LocalFrontend = "LocalFrontend";

            var frontendUrl = configuration["AppSettings:FrontendUrl"] ?? "http://localhost:3002";
            var allowedOrigins = new HashSet<string> { "http://localhost:3002", "http://127.0.0.1:3002", frontendUrl };

            services.AddCors(options =>
            {
                options.AddPolicy(LocalFrontend, p => p
                    .WithOrigins(allowedOrigins.ToArray())
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            });

            // Segunda barrera del login, por IP: el límite por usuario (LoginAttemptLimiter) no frena
            // a quien prueba muchos usuarios distintos, y cada intento abre una conexión a Oracle.
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(PoliticaLogin, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsJsonAsync(new ErrorResponseDto
                    {
                        StatusCode = StatusCodes.Status429TooManyRequests,
                        ErrorMessage = "Demasiados intentos de inicio de sesión desde este equipo. Espere un minuto."
                    }, cancellationToken);
                };
            });

            // En producción la API solo es alcanzable a través de nginx (red interna de Docker), así
            // que se confía en su X-Forwarded-For para conocer la IP real del cliente.
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = configuration.GetValue<bool>("Jwt:RequireHttpsMetadata", false);
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var errorResponse = new ErrorResponseDto
                        {
                            StatusCode = context.Response.StatusCode,
                            ErrorMessage = "No autorizado. Provea un token válido."
                        };
                        await context.Response.WriteAsJsonAsync(errorResponse);
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json";
                        var errorResponse = new ErrorResponseDto
                        {
                            StatusCode = context.Response.StatusCode,
                            ErrorMessage = "Prohibido. No tiene permiso para acceder a este recurso."
                        };
                        await context.Response.WriteAsJsonAsync(errorResponse);
                    }
                };
            });

            services.AddAuthorization();

            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations();
                options.SwaggerDoc("v1", new OpenApiInfo { Version = "v1", Title = "PortalGerencialApi" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT Authorization header usando el esquema Bearer."
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new List<string>()
                    }
                });
                options.UseInlineDefinitionsForEnums();
            });

            return services;
        }

        public static void UseDevelopmentConfiguration(this WebApplication app)
        {
            if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
            {
                app.UseSwagger();
                app.UseSwaggerUI(options => options.EnablePersistAuthorization());
            }
        }
    }
}
