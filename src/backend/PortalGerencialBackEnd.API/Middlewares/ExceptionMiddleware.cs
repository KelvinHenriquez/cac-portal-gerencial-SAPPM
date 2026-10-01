using System.Net;
using System.Text.Json;
using PortalGerencialBackEnd.Core.Dtos.Responses;
using PortalGerencialBackEnd.Core.Exceptions;

namespace PortalGerencialBackEnd.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private static readonly JsonSerializerOptions JsonOpciones = new(JsonSerializerDefaults.Web);

        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                NotFoundException => HttpStatusCode.NotFound,
                BadRequestException => HttpStatusCode.BadRequest,
                ForbiddenException => HttpStatusCode.Forbidden,
                ConflictException => HttpStatusCode.Conflict,
                TooManyRequestsException => HttpStatusCode.TooManyRequests,
                ServiceUnavailableException => HttpStatusCode.ServiceUnavailable,
                NotImplementedException => HttpStatusCode.NotImplemented,
                _ => HttpStatusCode.InternalServerError
            };

            if (statusCode == HttpStatusCode.InternalServerError)
                _logger.LogError(exception, "Error no controlado procesando {Path}", context.Request.Path);

            context.Response.StatusCode = (int)statusCode;

            var response = new ErrorResponseDto
            {
                StatusCode = (int)statusCode,
                ErrorMessage = statusCode == HttpStatusCode.InternalServerError
                    ? "Ocurrió un error inesperado. Revise el log del servidor."
                    : exception.Message
            };

            // camelCase como el resto de la API: el frontend lee apiError.errorMessage.
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOpciones));
        }
    }

    public static class ExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app) =>
            app.UseMiddleware<ExceptionMiddleware>();
    }
}
