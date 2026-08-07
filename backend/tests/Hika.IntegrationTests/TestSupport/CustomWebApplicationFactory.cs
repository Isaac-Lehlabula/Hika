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
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
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
}
