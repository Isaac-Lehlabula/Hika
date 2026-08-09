using System.Net;
using System.Net.Http.Json;
using Hika.IntegrationTests.TestSupport;
using Shouldly;

namespace Hika.IntegrationTests.RateLimiting;

/// <summary>Uses its own factory (RateLimitedWebApplicationFactory) with a tiny configured
/// "auth" limit — the shared CustomWebApplicationFactory used by every other integration test
/// overrides this to effectively unlimited (see its comment) so this is the only place the real
/// limiter behavior gets exercised.</summary>
public class RateLimitingEndpointsTests(RateLimitedWebApplicationFactory factory) : IClassFixture<RateLimitedWebApplicationFactory>
{
    [Fact]
    public async Task Login_ExceedingThePermitLimit_ReturnsTooManyRequests()
    {
        var client = factory.CreateClient();
        const string email = "ratelimit-login@example.com";
        const string password = "Passw0rd123";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "Rate",
            lastName = "Limit",
        });
        registerResponse.EnsureSuccessStatusCode();

        for (var i = 0; i < RateLimitedWebApplicationFactory.AuthPermitLimit; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
            response.StatusCode.ShouldBe(HttpStatusCode.OK, $"request {i + 1} should still be within the permit limit");
        }

        var throttledResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

        throttledResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        throttledResponse.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var body = await throttledResponse.Content.ReadAsStringAsync();
        body.ShouldContain("Too Many Requests");
    }
}
