using Hika.Application.Payments.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hika.IntegrationTests.TestSupport;

/// <summary>A separate factory (own Postgres container) with IPaymentGateway swapped for
/// FakePendingPaymentGateway and a known Ozow:PrivateKey, so OzowWebhooksEndpointsTests can
/// sign real webhook payloads and exercise the Ozow-shaped accept → AwaitingPayment →
/// webhook → Confirmed/Declined flow end-to-end, without any dependency on Ozow's actual API.</summary>
public sealed class PendingPaymentGatewayFactory : CustomWebApplicationFactory
{
    public const string PrivateKey = "integration-test-ozow-private-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ozow:PrivateKey"] = PrivateKey,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPaymentGateway>();
            services.AddScoped<IPaymentGateway, FakePendingPaymentGateway>();
        });
    }
}
