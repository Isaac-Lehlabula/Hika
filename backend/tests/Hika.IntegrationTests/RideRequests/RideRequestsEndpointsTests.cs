using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Hika.IntegrationTests.TestSupport;
using Shouldly;

namespace Hika.IntegrationTests.RideRequests;

public class RideRequestsEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly object[] JohannesburgToGiyaniStops =
    [
        new { rawName = "Johannesburg", province = "Gauteng" },
        new { rawName = "Polokwane", province = "Limpopo" },
    ];

    private static readonly DateTimeOffset TravelDeparture = DateTimeOffset.UtcNow.AddDays(10).Date.AddHours(5);
    private static readonly DateOnly TravelDate = DateOnly.FromDateTime(TravelDeparture.AddHours(2).Date);

    private async Task<(HttpClient DriverClient, Guid TripId)> CreateMatchingTripAsync([CallerMemberName] string testName = "")
    {
        var (driverClient, _, _) = await factory.CreateAuthenticatedClientAsync($"{testName}Driver");
        (await driverClient.PostAsJsonAsync("/api/v1/drivers/me/driver-profile", new
        {
            licenseNumber = "1234567890123",
            licenseExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
        })).EnsureSuccessStatusCode();

        var vehicleResponse = await driverClient.PostAsJsonAsync("/api/v1/drivers/me/vehicles", new
        {
            make = "Toyota",
            model = "Quantum",
            year = 2019,
            color = "White",
            registrationNumber = $"CA{Random.Shared.Next(100000, 999999)}",
            seatCapacity = 8,
        });
        var vehicle = await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>();

        var tripResponse = await driverClient.PostAsJsonAsync("/api/v1/trips", new
        {
            vehicleId = vehicle!.Id,
            departureAtUtc = TravelDeparture,
            totalSeatsOffered = 4,
            pricePerSeat = 300m,
            stops = JohannesburgToGiyaniStops,
        });
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripResponse>();

        return (driverClient, trip!.Id);
    }

    private static object PostRequest(decimal? proposedPricePerSeat = null) => new
    {
        origin = "Johannesburg",
        destination = "Polokwane",
        travelDate = TravelDate,
        seatsNeeded = 1,
        proposedPricePerSeat,
    };

    [Fact]
    public async Task CreateRequest_Valid_ReturnsOpenRequest()
    {
        var (riderClient, _, _) = await factory.CreateAuthenticatedClientAsync("Rider1");

        var response = await riderClient.PostAsJsonAsync("/api/v1/ride-requests", PostRequest(proposedPricePerSeat: 250m));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var request = await response.Content.ReadFromJsonAsync<RideRequestResponse>();
        request!.Status.ShouldBe("Open");
        request.IsExpired.ShouldBeFalse();
        request.ProposedPricePerSeat.ShouldBe(250m);
    }

    [Fact]
    public async Task GetOpenRequests_ShowsAnotherRidersOpenRequest()
    {
        var (riderClient, _, _) = await factory.CreateAuthenticatedClientAsync("Rider2");
        var created = await (await riderClient.PostAsJsonAsync("/api/v1/ride-requests", PostRequest())).Content.ReadFromJsonAsync<RideRequestResponse>();

        var (driverClient, _, _) = await factory.CreateAuthenticatedClientAsync("Driver2Browsing");
        var openRequests = await (await driverClient.GetAsync("/api/v1/ride-requests/open"))
            .Content.ReadFromJsonAsync<List<RideRequestResponse>>();

        openRequests!.ShouldContain(r => r.Id == created!.Id);
    }

    [Fact]
    public async Task Claim_MatchingTrip_ProducesAConfirmedBookingAndClosesTheRequest()
    {
        var (riderClient, riderUserId, _) = await factory.CreateAuthenticatedClientAsync("Rider3");
        var created = await (await riderClient.PostAsJsonAsync("/api/v1/ride-requests", PostRequest())).Content.ReadFromJsonAsync<RideRequestResponse>();

        var (driverClient, tripId) = await CreateMatchingTripAsync();

        var claimResponse = await driverClient.PostAsJsonAsync(
            $"/api/v1/ride-requests/{created!.Id}/claim", new { tripId, boardingStopSequence = 0, alightingStopSequence = 1 });

        claimResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var booking = await claimResponse.Content.ReadFromJsonAsync<BookingResponse>();
        booking!.Status.ShouldBe("Confirmed");

        var myRequests = await (await riderClient.GetAsync("/api/v1/ride-requests/me")).Content.ReadFromJsonAsync<List<RideRequestResponse>>();
        var claimed = myRequests!.Single(r => r.Id == created.Id);
        claimed.Status.ShouldBe("Claimed");
        claimed.ClaimedBookingId.ShouldBe(booking.Id);

        // Claiming reuses the exact same Request->Accept pipeline a normal search-and-request
        // does, so the post-accept chat should already be open on the resulting booking.
        var conversation = await (await riderClient.GetAsync($"/api/v1/bookings/{booking.Id}/conversation"))
            .Content.ReadFromJsonAsync<ConversationResponse>();
        conversation!.IsOpen.ShouldBeTrue();

        var openRequests = await (await driverClient.GetAsync("/api/v1/ride-requests/open"))
            .Content.ReadFromJsonAsync<List<RideRequestResponse>>();
        openRequests!.ShouldNotContain(r => r.Id == created.Id);
    }

    [Fact]
    public async Task Claim_TripOnADifferentDate_ReturnsBadRequest()
    {
        var (riderClient, _, _) = await factory.CreateAuthenticatedClientAsync("Rider4");
        var created = await (await riderClient.PostAsJsonAsync("/api/v1/ride-requests", PostRequest())).Content.ReadFromJsonAsync<RideRequestResponse>();

        var (driverClient, _, _) = await factory.CreateAuthenticatedClientAsync("Driver4");
        (await driverClient.PostAsJsonAsync("/api/v1/drivers/me/driver-profile", new
        {
            licenseNumber = "1234567890123",
            licenseExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
        })).EnsureSuccessStatusCode();
        var vehicle = await (await driverClient.PostAsJsonAsync("/api/v1/drivers/me/vehicles", new
        {
            make = "Toyota",
            model = "Quantum",
            year = 2019,
            color = "White",
            registrationNumber = $"CA{Random.Shared.Next(100000, 999999)}",
            seatCapacity = 8,
        })).Content.ReadFromJsonAsync<VehicleResponse>();
        var mismatchedTrip = await (await driverClient.PostAsJsonAsync("/api/v1/trips", new
        {
            vehicleId = vehicle!.Id,
            departureAtUtc = TravelDeparture.AddDays(5),
            totalSeatsOffered = 4,
            pricePerSeat = 300m,
            stops = JohannesburgToGiyaniStops,
        })).Content.ReadFromJsonAsync<TripResponse>();

        var claimResponse = await driverClient.PostAsJsonAsync(
            $"/api/v1/ride-requests/{created!.Id}/claim",
            new { tripId = mismatchedTrip!.Id, boardingStopSequence = 0, alightingStopSequence = 1 });

        claimResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Claim_SomeoneElsesTrip_ReturnsNotFound()
    {
        var (riderClient, _, _) = await factory.CreateAuthenticatedClientAsync("Rider5");
        var created = await (await riderClient.PostAsJsonAsync("/api/v1/ride-requests", PostRequest())).Content.ReadFromJsonAsync<RideRequestResponse>();

        var (_, tripId) = await CreateMatchingTripAsync();
        var (otherDriverClient, _, _) = await factory.CreateAuthenticatedClientAsync("OtherDriver5");

        var claimResponse = await otherDriverClient.PostAsJsonAsync(
            $"/api/v1/ride-requests/{created!.Id}/claim", new { tripId, boardingStopSequence = 0, alightingStopSequence = 1 });

        claimResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelRequest_OwnOpenRequest_BecomesCancelled()
    {
        var (riderClient, _, _) = await factory.CreateAuthenticatedClientAsync("Rider6");
        var created = await (await riderClient.PostAsJsonAsync("/api/v1/ride-requests", PostRequest())).Content.ReadFromJsonAsync<RideRequestResponse>();

        var deleteResponse = await riderClient.DeleteAsync($"/api/v1/ride-requests/{created!.Id}");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var myRequests = await (await riderClient.GetAsync("/api/v1/ride-requests/me")).Content.ReadFromJsonAsync<List<RideRequestResponse>>();
        myRequests!.Single(r => r.Id == created.Id).Status.ShouldBe("Cancelled");
    }

    [Fact]
    public async Task CancelRequest_NotTheOwner_ReturnsNotFound()
    {
        var (riderClient, _, _) = await factory.CreateAuthenticatedClientAsync("Rider7");
        var created = await (await riderClient.PostAsJsonAsync("/api/v1/ride-requests", PostRequest())).Content.ReadFromJsonAsync<RideRequestResponse>();

        var (strangerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Stranger7");
        var deleteResponse = await strangerClient.DeleteAsync($"/api/v1/ride-requests/{created!.Id}");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private sealed record VehicleResponse(Guid Id);

    private sealed record TripResponse(Guid Id);

    private sealed record BookingResponse(Guid Id, string Status);

    private sealed record ConversationResponse(Guid Id, Guid BookingId, bool IsOpen);

    private sealed record RideRequestResponse(
        Guid Id, string OriginRawText, string DestinationRawText, DateOnly TravelDate, int SeatsNeeded,
        decimal? ProposedPricePerSeat, string Status, bool IsExpired, Guid? ClaimedBookingId);
}
