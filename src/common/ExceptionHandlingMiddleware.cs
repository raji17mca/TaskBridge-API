using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TaskBridge_API.Common;

/// <summary>Converts unhandled exceptions into ProblemDetails responses without leaking internal details.</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Runs the rest of the pipeline, converting any unhandled exception into a ProblemDetails response.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request to {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Invalid request", ex.Message);
        }
        catch (TenantResolutionException ex)
        {
            _logger.LogWarning(ex, "Tenant resolution failed for {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized", "Tenant could not be resolved from the request.");
        }
        catch (UserResolutionException ex)
        {
            _logger.LogWarning(ex, "User resolution failed for {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized", "User could not be resolved from the request.");
        }
        catch (Exception ex)
        {
            // Deliberately generic: never surface stack traces or internal exception messages to clients.
            _logger.LogError(ex, "Unhandled exception for {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred", "Please contact support if the problem persists.");
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;
        var problem = new ProblemDetails { Status = statusCode, Title = title, Detail = detail };
        return context.Response.WriteAsJsonAsync(problem);
    }
}
