using System.Net.Http.Json;
using Hika.Application.Users.Ports;
using Hika.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Hika.IntegrationTests.TestSupport;

/// <summary>
/// Spins up a real Postgres container (Testcontainers) and a fully-wired Hika.Api host against
/// it, with the email/SMS senders swapped for in-memory capturing fakes so tests can assert on
/// what would have been sent without needing Mailhog or a real SMTP/SMS provider running.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("hika_test")
        .WithUsername("hika_test")
        .WithPassword("hika_test")
        .Build();

    public CapturingEmailSender EmailSender { get; } = new();

    public CapturingSmsSender SmsSender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Jwt:SigningKey"] = "integration-test-signing-key-at-least-32-bytes-long",
                ["Jwt:Issuer"] = "hika-api-test",
                ["Jwt:Audience"] = "hika-client-test",
                ["Frontend:BaseUrl"] = "http://localhost:3000",
                ["Smtp:Host"] = "unused-in-tests",
                ["Smtp:From"] = "no-reply@hika.local",
                ["LocalFileStorage:RootPath"] = Path.Combine(Path.GetTempPath(), "hika-integration-tests", Guid.NewGuid().ToString("N")),
                ["LocalFileStorage:PublicBaseUrl"] = "http://localhost",

                // TestServer gives every in-memory request the same loopback RemoteIpAddress,
                // so the "auth" rate-limit policy's per-IP partition would otherwise see every
                // test in a class (often dozens of CreateAuthenticatedClientAsync calls sharing
                // this one factory) as a single caller. Effectively unlimited here; see
                // RateLimitingEndpointsTests for the dedicated test that exercises the real limits.
                ["RateLimiting:AuthPermitLimit"] = "100000",
                ["RateLimiting:ReportsPermitLimit"] = "100000",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);

            services.RemoveAll<ISmsSender>();
            services.AddSingleton<ISmsSender>(SmsSender);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    /// <summary>Registers, verifies, and logs in a fresh user — for tests of endpoints that
    /// just need *some* authenticated caller, not the auth flow itself (see AuthEndpointsTests
    /// for that).</summary>
    public async Task<(HttpClient Client, Guid UserId, string AccessToken)> CreateAuthenticatedClientAsync(
        [System.Runtime.CompilerServices.CallerMemberName] string testName = "")
    {
        var client = CreateClient();
        var email = $"{testName.ToLowerInvariant()}-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Passw0rd123",
            firstName = "Test",
            lastName = "Driver",
        });
        registerResponse.EnsureSuccessStatusCode();
        var userId = (await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>())!.UserId;

        var verificationEmail = EmailSender.SentEmails.Single(e => e.To == email);
        var token = CapturingEmailSender.ExtractQueryParam(verificationEmail.Body, "token");
        (await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { userId, token })).EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Passw0rd123" });
        loginResponse.EnsureSuccessStatusCode();
        var tokens = (await loginResponse.Content.ReadFromJsonAsync<TokenResponse>())!;

        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);

        return (client, userId, tokens.AccessToken);
    }

    /// <summary>Grants admin access directly via the database — there is deliberately no HTTP
    /// endpoint for this (see UserProfile.GrantAdmin's remarks), so tests of Admin-policy-gated
    /// endpoints promote their caller this way, mirroring how Program.cs's dev-only
    /// Admin:BootstrapEmail step does it in a real environment.</summary>
    public async Task PromoteToAdminAsync(Guid userId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await db.UserProfiles.SingleAsync(p => p.Id == userId);
        profile.GrantAdmin();
        await db.SaveChangesAsync();
    }

    private sealed record RegisterResponse(Guid UserId);

    private sealed record TokenResponse(string AccessToken);
}
