using System.Threading.RateLimiting;
using Hika.Api.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Hika.Api.RateLimiting;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string ReportsPolicy = "reports";

    public static IServiceCollection AddHikaRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitOptions>()
            .Bind(configuration.GetSection(RateLimitOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Read settings fresh per partition creation (not captured at startup) so
            // CustomWebApplicationFactory's config override and IOptionsMonitor reloads both
            // take effect without rebuilding the limiter.
            options.AddPolicy(AuthPolicy, httpContext =>
            {
                var settings = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<RateLimitOptions>>().CurrentValue;
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.AuthPermitLimit,
                    Window = TimeSpan.FromSeconds(settings.AuthWindowSeconds),
                    QueueLimit = 0,
                });
            });

            options.AddPolicy(ReportsPolicy, httpContext =>
            {
                var settings = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<RateLimitOptions>>().CurrentValue;
                var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
                    ? httpContext.User.GetUserId().ToString()
                    : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = settings.ReportsPermitLimit,
                    Window = TimeSpan.FromSeconds(settings.ReportsWindowSeconds),
                    QueueLimit = 0,
                });
            });

            // Matches GlobalExceptionHandler's ProblemDetails shape — a rejected request never
            // reaches an exception handler (it's short-circuited by the limiter middleware), so
            // this is the one place that shape has to be produced separately.
            options.OnRejected = async (context, cancellationToken) =>
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = "You've made too many requests. Try again shortly.",
                };
                problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                // The contentType parameter here is what actually sets the response
                // Content-Type — setting Response.ContentType beforehand gets overwritten by
                // WriteAsJsonAsync's own default ("application/json") otherwise.
                await context.HttpContext.Response.WriteAsJsonAsync(
                    problemDetails, options: null, contentType: "application/problem+json", cancellationToken);
            };
        });

        return services;
    }
}
