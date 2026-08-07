using System.Net;
using System.Net.Http.Json;
using Hika.IntegrationTests.TestSupport;
using Shouldly;

namespace Hika.IntegrationTests.Trips;

public class TripsEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static object ValidVehicleBody() => new
    {
        make = "Toyota",
        model = "Corolla",
        year = 2020,
        color = "White",
        registrationNumber = $"CA{Random.Shared.Next(100000, 999999)}",
        seatCapacity = 4,
    };

    private static object ValidTripBody(Guid vehicleId, int totalSeatsOffered = 4) => new
    {
        vehicleId,
        departureAtUtc = DateTimeOffset.UtcNow.AddDays(1),
        totalSeatsOffered,
        pricePerSeat = 300m,
        luggageAllowance = "One bag per passenger",
        notes = "No smoking",
        stops = new object[]
        {
            new { rawName = "Johannesburg", province = "Gauteng" },
            new { rawName = "Polokwane", province = "Limpopo" },
            new { rawName = "Giyani", province = "Limpopo" },
        },
    };

    private async Task<(HttpClient Client, Guid VehicleId)> CreateClientWithVehicleAsync(
        [System.Runtime.CompilerServices.CallerMemberName] string testName = "")
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync(testName);
        (await client.PostAsJsonAsync("/api/v1/drivers/me/driver-profile", new
        {
            licenseNumber = "1234567890123",
            licenseExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
        })).EnsureSuccessStatusCode();

        var vehicleResponse = await client.PostAsJsonAsync("/api/v1/drivers/me/vehicles", ValidVehicleBody());
        vehicleResponse.EnsureSuccessStatusCode();
        var vehicle = await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>();

        return (client, vehicle!.Id);
    }

    [Fact]
    public async Task CreateTrip_WithoutDriverProfile_ReturnsBadRequest()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/trips", ValidTripBody(Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTrip_SeatsExceedVehicleCapacity_ReturnsBadRequest()
    {
        var (client, vehicleId) = await CreateClientWithVehicleAsync();

        var response = await client.PostAsJsonAsync("/api/v1/trips", ValidTripBody(vehicleId, totalSeatsOffered: 10));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTrip_Valid_ReturnsCreatedWithStopsAndSegments()
    {
        var (client, vehicleId) = await CreateClientWithVehicleAsync();

        var response = await client.PostAsJsonAsync("/api/v1/trips", ValidTripBody(vehicleId));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var trip = await response.Content.ReadFromJsonAsync<TripResponse>();
        trip!.Status.ShouldBe("Scheduled");
        trip.Stops.Count.ShouldBe(3);
        trip.Segments.Count.ShouldBe(2);
        trip.Segments.ShouldAllBe(s => s.SeatsAvailable == 4);
        trip.PricePerSeat.ShouldBe(300m);
    }

    [Fact]
    public async Task GetTrip_Anonymous_Succeeds()
    {
        var (client, vehicleId) = await CreateClientWithVehicleAsync();
        var createResponse = await client.PostAsJsonAsync("/api/v1/trips", ValidTripBody(vehicleId));
        var created = await createResponse.Content.ReadFromJsonAsync<TripResponse>();

        var anonymousClient = factory.CreateClient();
        var response = await anonymousClient.GetAsync($"/api/v1/trips/{created!.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var trip = await response.Content.ReadFromJsonAsync<TripResponse>();
        trip!.Id.ShouldBe(created.Id);
        trip.Driver.FirstName.ShouldBe("Test");
    }

    [Fact]
    public async Task GetTrip_UnknownId_ReturnsNotFound()
    {
        var anonymousClient = factory.CreateClient();

        var response = await anonymousClient.GetAsync($"/api/v1/trips/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyTrips_OnlyReturnsCallersOwn()
    {
        var (client, vehicleId) = await CreateClientWithVehicleAsync();
        await client.PostAsJsonAsync("/api/v1/trips", ValidTripBody(vehicleId));
        await client.PostAsJsonAsync("/api/v1/trips", ValidTripBody(vehicleId));

        var (otherClient, otherVehicleId) = await CreateClientWithVehicleAsync("OtherDriver");
        await otherClient.PostAsJsonAsync("/api/v1/trips", ValidTripBody(otherVehicleId));

        var response = await client.GetAsync("/api/v1/trips/me");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var trips = await response.Content.ReadFromJsonAsync<List<TripSummaryResponse>>();
        trips!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CancelTrip_Owner_SetsStatusToCancelled()
    {
        var (client, vehicleId) = await CreateClientWithVehicleAsync();
        var createResponse = await client.PostAsJsonAsync("/api/v1/trips", ValidTripBody(vehicleId));
        var created = await createResponse.Content.ReadFromJsonAsync<TripResponse>();

        var cancelResponse = await client.PostAsync($"/api/v1/trips/{created!.Id}/cancel", null);
        cancelResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/v1/trips/{created.Id}");
        var trip = await getResponse.Content.ReadFromJsonAsync<TripResponse>();
        trip!.Status.ShouldBe("Cancelled");
    }

    [Fact]
    public async Task CancelTrip_NotOwner_ReturnsNotFound()
    {
        var (client, vehicleId) = await CreateClientWithVehicleAsync();
        var createResponse = await client.PostAsJsonAsync("/api/v1/trips", ValidTripBody(vehicleId));
        var created = await createResponse.Content.ReadFromJsonAsync<TripResponse>();

        var (otherClient, _, _) = await factory.CreateAuthenticatedClientAsync("NotOwner");
        var response = await otherClient.PostAsync($"/api/v1/trips/{created!.Id}/cancel", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private sealed record VehicleResponse(Guid Id);

    private sealed record TripStopResponse(int Sequence, Guid? LocationId, string Name, string Province);

    private sealed record TripSegmentResponse(int FromSequence, int ToSequence, int SeatsAvailable);

    private sealed record TripDriverSummary(Guid UserId, string FirstName, string LastName);

    private sealed record TripVehicleSummary(Guid Id);

    private sealed record TripResponse(
        Guid Id, string Status, DateTimeOffset DepartureAtUtc, int TotalSeatsOffered, decimal PricePerSeat,
        TripDriverSummary Driver, TripVehicleSummary Vehicle,
        List<TripStopResponse> Stops, List<TripSegmentResponse> Segments);

    private sealed record TripSummaryResponse(Guid Id, string Status, string OriginName, string DestinationName);
}
