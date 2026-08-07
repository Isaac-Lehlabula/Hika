using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Hika.IntegrationTests.TestSupport;
using Shouldly;

namespace Hika.IntegrationTests.RideAlerts;

public class RideAlertsEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private async Task<HttpClient> CreateDriverClientAsync([CallerMemberName] string testName = "")
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync(testName);
        (await client.PostAsJsonAsync("/api/v1/drivers/me/driver-profile", new
        {
            licenseNumber = "1234567890123",
            licenseExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
        })).EnsureSuccessStatusCode();

        var vehicleResponse = await client.PostAsJsonAsync("/api/v1/drivers/me/vehicles", new
        {
            make = "Toyota",
            model = "Quantum",
            year = 2019,
            color = "White",
            registrationNumber = $"CA{Random.Shared.Next(100000, 999999)}",
            seatCapacity = 8,
        });
        vehicleResponse.EnsureSuccessStatusCode();
        return client;
    }

    private async Task PostTripAsync(HttpClient driverClient, DateTimeOffset departureAtUtc, object[] stops)
    {
        var vehicleResponse = await driverClient.GetAsync("/api/v1/drivers/me/vehicles");
        var vehicles = await vehicleResponse.Content.ReadFromJsonAsync<List<VehicleResponse>>();

        var response = await driverClient.PostAsJsonAsync("/api/v1/trips", new
        {
            vehicleId = vehicles!.Single().Id,
            departureAtUtc,
            totalSeatsOffered = 4,
            pricePerSeat = 300m,
            stops,
        });
        response.EnsureSuccessStatusCode();
    }

    private static readonly object[] JohannesburgToGiyaniStops =
    [
        new { rawName = "Johannesburg", province = "Gauteng" },
        new { rawName = "Polokwane", province = "Limpopo" },
        new { rawName = "Giyani", province = "Limpopo" },
    ];

    private static readonly object[] CapeTownToDurbanStops =
    [
        new { rawName = "Cape Town", province = "WesternCape" },
        new { rawName = "Durban", province = "KwaZuluNatal" },
    ];

    [Fact]
    public async Task CreateAlert_ReturnsActiveAlert()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/ride-alerts", new { origin = "Johannesburg", destination = "Giyani" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var alert = await response.Content.ReadFromJsonAsync<RideAlertResponse>();
        alert!.Status.ShouldBe("Active");
    }

    [Fact]
    public async Task GetMyAlerts_OnlyReturnsCallersOwn()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync("AlertOwner1");
        await client.PostAsJsonAsync("/api/v1/ride-alerts", new { origin = "Johannesburg", destination = "Giyani" });

        var (otherClient, _, _) = await factory.CreateAuthenticatedClientAsync("AlertOwner2");
        await otherClient.PostAsJsonAsync("/api/v1/ride-alerts", new { origin = "Cape Town", destination = "Durban" });

        var response = await client.GetAsync("/api/v1/ride-alerts/me");

        var alerts = await response.Content.ReadFromJsonAsync<List<RideAlertResponse>>();
        alerts!.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAlert_ByNonOwner_ReturnsNotFound()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync("AlertOwner3");
        var createResponse = await client.PostAsJsonAsync("/api/v1/ride-alerts", new { origin = "Johannesburg", destination = "Giyani" });
        var alert = await createResponse.Content.ReadFromJsonAsync<RideAlertResponse>();

        var (strangerClient, _, _) = await factory.CreateAuthenticatedClientAsync("AlertStranger");
        var response = await strangerClient.DeleteAsync($"/api/v1/ride-alerts/{alert!.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAlert_ByOwner_RemovesIt()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync("AlertOwner4");
        var createResponse = await client.PostAsJsonAsync("/api/v1/ride-alerts", new { origin = "Johannesburg", destination = "Giyani" });
        var alert = await createResponse.Content.ReadFromJsonAsync<RideAlertResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/ride-alerts/{alert!.Id}");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var remaining = await (await client.GetAsync("/api/v1/ride-alerts/me")).Content.ReadFromJsonAsync<List<RideAlertResponse>>();
        remaining!.ShouldBeEmpty();
    }

    [Fact]
    public async Task PostingAMatchingTrip_FulfillsTheAlertAndNotifiesItsOwner()
    {
        var (alertOwnerClient, _, _) = await factory.CreateAuthenticatedClientAsync("MatchAlertOwner");
        var createResponse = await alertOwnerClient.PostAsJsonAsync(
            "/api/v1/ride-alerts", new { origin = "Johannesburg", destination = "Giyani" });
        var alert = await createResponse.Content.ReadFromJsonAsync<RideAlertResponse>();

        var driverClient = await CreateDriverClientAsync("MatchAlertDriver");
        await PostTripAsync(driverClient, DateTimeOffset.UtcNow.AddDays(5), JohannesburgToGiyaniStops);

        var alertsResponse = await alertOwnerClient.GetAsync("/api/v1/ride-alerts/me");
        var alerts = await alertsResponse.Content.ReadFromJsonAsync<List<RideAlertResponse>>();
        alerts!.Single(a => a.Id == alert!.Id).Status.ShouldBe("Fulfilled");

        var notificationsResponse = await alertOwnerClient.GetAsync("/api/v1/notifications/me?page=1&pageSize=20");
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<PagedNotificationsResponse>();
        notifications!.Items.ShouldContain(n => n.Type == "RideAlertMatched");
    }

    [Fact]
    public async Task PostingAnUnrelatedTrip_DoesNotFulfillTheAlert()
    {
        var (alertOwnerClient, _, _) = await factory.CreateAuthenticatedClientAsync("NoMatchAlertOwner");
        var createResponse = await alertOwnerClient.PostAsJsonAsync(
            "/api/v1/ride-alerts", new { origin = "Johannesburg", destination = "Giyani" });
        var alert = await createResponse.Content.ReadFromJsonAsync<RideAlertResponse>();

        var driverClient = await CreateDriverClientAsync("NoMatchAlertDriver");
        await PostTripAsync(driverClient, DateTimeOffset.UtcNow.AddDays(5), CapeTownToDurbanStops);

        var alertsResponse = await alertOwnerClient.GetAsync("/api/v1/ride-alerts/me");
        var alerts = await alertsResponse.Content.ReadFromJsonAsync<List<RideAlertResponse>>();
        alerts!.Single(a => a.Id == alert!.Id).Status.ShouldBe("Active");
    }

    [Fact]
    public async Task DateSpecificAlert_OnlyMatchesTripsOnThatDay()
    {
        var (alertOwnerClient, _, _) = await factory.CreateAuthenticatedClientAsync("DateAlertOwner");
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var createResponse = await alertOwnerClient.PostAsJsonAsync(
            "/api/v1/ride-alerts", new { origin = "Johannesburg", destination = "Giyani", travelDate = targetDate });
        var alert = await createResponse.Content.ReadFromJsonAsync<RideAlertResponse>();

        var driverClient = await CreateDriverClientAsync("DateAlertDriver");
        await PostTripAsync(driverClient, DateTimeOffset.UtcNow.AddDays(20), JohannesburgToGiyaniStops);

        var alertsResponse = await alertOwnerClient.GetAsync("/api/v1/ride-alerts/me");
        var alerts = await alertsResponse.Content.ReadFromJsonAsync<List<RideAlertResponse>>();
        alerts!.Single(a => a.Id == alert!.Id).Status.ShouldBe("Active");
    }

    private sealed record VehicleResponse(Guid Id);

    private sealed record RideAlertResponse(Guid Id, string OriginRawText, string DestinationRawText, DateOnly? TravelDate, string Status);

    private sealed record NotificationItem(Guid Id, string Type, string Message, string Status);

    private sealed record PagedNotificationsResponse(List<NotificationItem> Items, int Page, int PageSize, int TotalCount);
}
