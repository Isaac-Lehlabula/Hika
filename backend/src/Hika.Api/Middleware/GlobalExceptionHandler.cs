using Hika.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Middleware;

/// <summary>
/// Single place that turns exceptions into RFC 7807 ProblemDetails. Domain/application
/// exceptions map to a specific status; anything unexpected becomes a generic 500 with
/// no internal detail leaked to the client (full detail still goes to the server log).
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            AppValidationException => (StatusCodes.Status400BadRequest, "Validation Failed"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "{ExceptionType} while processing {Method} {Path}",
                exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        ProblemDetails problemDetails = exception is AppValidationException validationException
            ? new ValidationProblemDetails(new Dictionary<string, string[]>(validationException.Errors))
            {
                Status = statusCode,
                Title = title,
            }
            : new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError ? null : exception.Message,
            };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        // Serialize using the runtime type (not the compile-time `ProblemDetails` type) so
        // ValidationProblemDetails.Errors is actually included in the response body.
        await httpContext.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType(), cancellationToken);

        return true;
    }
}
