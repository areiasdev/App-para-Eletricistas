using System.Text.Json;
using TecnicoApp.Application.Common.Exceptions;

namespace TecnicoApp.API.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Erro de validação: {Errors}", ex.Errors);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var problem = new
            {
                type = "https://tecnicoapp.pt/errors/validation",
                title = "Erro de validação",
                status = 400,
                errors = ex.Errors,
                traceId = context.TraceIdentifier
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problem = new
            {
                type = "https://tecnicoapp.pt/errors/internal",
                title = "Ocorreu um erro inesperado.",
                status = 500,
                traceId = context.TraceIdentifier
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
        }
    }
}
