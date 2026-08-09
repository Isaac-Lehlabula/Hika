using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Hika.IntegrationTests.TestSupport;

/// <summary>
/// A separate factory (its own Postgres container, its own rate-limiter state) with the "auth"
/// policy dialed down to a handful of requests per window, specifically so
/// RateLimitingEndpointsTests can trip it without needing hundreds of requests. Everything else
/// shares CustomWebApplicationFactory's setup — this only adds one more config source on top,
/// which wins over the base class's effectively-unlimited override since config sources are
/// applied in the order they're added.
/// </summary>
public sealed class RateLimitedWebApplicationFactory : CustomWebApplicationFactory
{
    public const int AuthPermitLimit = 3;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:AuthPermitLimit"] = AuthPermitLimit.ToString(),
                ["RateLimiting:AuthWindowSeconds"] = "60",
            });
        });
    }
}
