using System.ComponentModel.DataAnnotations;

namespace Hika.Api.RateLimiting;

/// <summary>
/// Configurable so integration tests can override these to an effectively-unlimited value
/// (see CustomWebApplicationFactory) without the production defaults having to be that loose,
/// and so a dedicated rate-limiter test can dial them down to something small enough to trip
/// in a handful of requests.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Applied to login, phone-OTP request/verify, and password-reset request/confirm
    /// — the classic brute-force/enumeration targets, see docs/security.md. Partitioned by
    /// caller IP, since these are all anonymous-accessible endpoints.</summary>
    [Range(1, int.MaxValue)]
    public int AuthPermitLimit { get; init; } = 10;

    [Range(1, int.MaxValue)]
    public int AuthWindowSeconds { get; init; } = 60;

    /// <summary>Applied to filing a report and blocking/unblocking a user — reduces
    /// harassment-via-reporting abuse (see docs/security.md). Partitioned by the caller's user
    /// id, since these endpoints require authentication.</summary>
    [Range(1, int.MaxValue)]
    public int ReportsPermitLimit { get; init; } = 20;

    [Range(1, int.MaxValue)]
    public int ReportsWindowSeconds { get; init; } = 60;
}
