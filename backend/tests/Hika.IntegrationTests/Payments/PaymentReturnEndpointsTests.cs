using System.Net;
using Hika.IntegrationTests.TestSupport;
using Shouldly;

namespace Hika.IntegrationTests.Payments;

/// <summary>The unauthenticated pages Ozow redirects the passenger's browser to after checkout —
/// see PaymentReturnController's remarks.</summary>
public class PaymentReturnEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Theory]
    [InlineData("/payment-return/success", "You're all set")]
    [InlineData("/payment-return/cancel", "Payment cancelled")]
    [InlineData("/payment-return/error", "Something went wrong")]
    public async Task PaymentReturnPage_ReturnsHtmlWithExpectedHeading(string path, string expectedHeading)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain(expectedHeading);
    }
}
