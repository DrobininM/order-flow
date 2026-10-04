using System.Net;
using System.Text.Json;

namespace PaymentService.Presentation.Middleware;

/// <summary>
/// Global exception handler. Validation failures are returned as 400 with details, while
/// unexpected exceptions return a generic 500 payload in Production and full detail otherwise.
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (FluentValidation.ValidationException validationEx)
        {
            _logger.LogWarning(validationEx, "Validation error.");

            var errors = validationEx.Errors
                .Select(e => new { Code = e.ErrorCode, e.PropertyName, e.ErrorMessage })
                .ToList();

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(new { Errors = errors }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = (object)(_env.IsProduction()
                ? new { Error = new { Code = "Internal.Error", Message = "An unexpected error occurred." } }
                : new { Error = new { Code = "Internal.Error", Message = ex.Message, Detail = ex.ToString() } });

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}

internal static class ErrorHandlingMiddlewareExtensions
{
    internal static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
