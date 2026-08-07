using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Hika.Domain.Trips;
using Hika.Infrastructure.Persistence;
using Hika.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Hika.IntegrationTests.Bookings;

public class BookingsEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly object[] JohannesburgToGiyaniStops =
    [
        new { rawName = "Johannesburg", province = "Gauteng" },
        new { rawName = "Polokwane", province = "Limpopo" },
        new { rawName = "Giyani", province = "Limpopo" },
    ];

    private async Task<(HttpClient DriverClient, Guid DriverUserId, Guid TripId)> CreateTripAsync(
        int seats = 4, [CallerMemberName] string testName = "")
    {
        var (client, driverUserId, _) = await factory.CreateAuthenticatedClientAsync(testName);
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
        var vehicle = await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>();

        var tripResponse = await client.PostAsJsonAsync("/api/v1/trips", new
        {
            vehicleId = vehicle!.Id,
            departureAtUtc = DateTimeOffset.UtcNow.AddDays(3),
            totalSeatsOffered = seats,
            pricePerSeat = 300m,
            stops = JohannesburgToGiyaniStops,
        });
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripResponse>();

        return (client, driverUserId, trip!.Id);
    }

    private static object BookJohannesburgToPolokwane(Guid tripId, int seats) => new
    {
        tripId,
        boardingStopSequence = 0,
        alightingStopSequence = 1,
        seatsRequested = seats,
    };

    private static object BookPolokwaneToGiyani(Guid tripId, int seats) => new
    {
        tripId,
        boardingStopSequence = 1,
        alightingStopSequence = 2,
        seatsRequested = seats,
    };

    [Fact]
    public async Task RequestBooking_Valid_ReturnsPendingAndOnlyReducesCoveredSegment()
    {
        var (_, _, tripId) = await CreateTripAsync(seats: 4);
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger1");

        var response = await passengerClient.PostAsJsonAsync("/api/v1/bookings", BookJohannesburgToPolokwane(tripId, 2));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var booking = await response.Content.ReadFromJsonAsync<BookingResponse>();
        booking!.Status.ShouldBe("Pending");
        booking.SeatsRequested.ShouldBe(2);
        booking.TotalPrice.ShouldBe(600m);
        booking.BoardingStopName.ShouldBe("Johannesburg");
        booking.AlightingStopName.ShouldBe("Polokwane");

        var tripResponse = await passengerClient.GetAsync($"/api/v1/trips/{tripId}");
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDetailResponse>();
        var johannesburgToPolokwane = trip!.Segments.Single(s => s.FromSequence == 0 && s.ToSequence == 1);
        var polokwaneToGiyani = trip.Segments.Single(s => s.FromSequence == 1 && s.ToSequence == 2);
        johannesburgToPolokwane.SeatsAvailable.ShouldBe(2);
        polokwaneToGiyani.SeatsAvailable.ShouldBe(4);
    }

    [Fact]
    public async Task RequestBooking_MoreSeatsThanAvailable_ReturnsConflict()
    {
        var (_, _, tripId) = await CreateTripAsync(seats: 2);
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger2");

        var response = await passengerClient.PostAsJsonAsync("/api/v1/bookings", BookJohannesburgToPolokwane(tripId, 3));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RequestBooking_OwnTrip_ReturnsBadRequest()
    {
        var (driverClient, _, tripId) = await CreateTripAsync();

        var response = await driverClient.PostAsJsonAsync("/api/v1/bookings", BookJohannesburgToPolokwane(tripId, 1));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AcceptBooking_Owner_SetsConfirmed()
    {
        var (driverClient, _, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger3");
        var created = await RequestBookingAsync(passengerClient, tripId, 1);

        var response = await driverClient.PostAsync($"/api/v1/bookings/{created.Id}/accept", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var booking = await response.Content.ReadFromJsonAsync<BookingResponse>();
        booking!.Status.ShouldBe("Confirmed");
    }

    [Fact]
    public async Task AcceptBooking_NotTripOwner_ReturnsNotFound()
    {
        var (_, _, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger4");
        var created = await RequestBookingAsync(passengerClient, tripId, 1);

        var (otherClient, _, _) = await factory.CreateAuthenticatedClientAsync("NotTheDriver");
        var response = await otherClient.PostAsync($"/api/v1/bookings/{created.Id}/accept", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeclineBooking_ReleasesSeatsBackToTheSegment()
    {
        var (driverClient, _, tripId) = await CreateTripAsync(seats: 3);
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger5");
        var created = await RequestBookingAsync(passengerClient, tripId, 3);

        var declineResponse = await driverClient.PostAsync($"/api/v1/bookings/{created.Id}/decline", null);
        declineResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var declined = await declineResponse.Content.ReadFromJsonAsync<BookingResponse>();
        declined!.Status.ShouldBe("Declined");

        var tripResponse = await passengerClient.GetAsync($"/api/v1/trips/{tripId}");
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDetailResponse>();
        trip!.Segments.Single(s => s.FromSequence == 0 && s.ToSequence == 1).SeatsAvailable.ShouldBe(3);
    }

    [Fact]
    public async Task CancelBooking_ByPassenger_ReleasesSeatsAndSetsCancelled()
    {
        var (_, _, tripId) = await CreateTripAsync(seats: 3);
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger6");
        var created = await RequestBookingAsync(passengerClient, tripId, 2);

        var cancelResponse = await passengerClient.PostAsJsonAsync($"/api/v1/bookings/{created.Id}/cancel", new { reason = "Change of plans" });

        cancelResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<BookingResponse>();
        cancelled!.Status.ShouldBe("Cancelled");
        cancelled.CancellationReason.ShouldBe("Change of plans");

        var tripResponse = await passengerClient.GetAsync($"/api/v1/trips/{tripId}");
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDetailResponse>();
        trip!.Segments.Single(s => s.FromSequence == 0 && s.ToSequence == 1).SeatsAvailable.ShouldBe(3);
    }

    [Fact]
    public async Task CancelBooking_ByAnotherUser_ReturnsNotFound()
    {
        var (_, _, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger7");
        var created = await RequestBookingAsync(passengerClient, tripId, 1);

        var (otherClient, _, _) = await factory.CreateAuthenticatedClientAsync("NotThePassenger");
        var response = await otherClient.PostAsJsonAsync($"/api/v1/bookings/{created.Id}/cancel", new { });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteBooking_BeforeDeparture_ReturnsBadRequest()
    {
        var (driverClient, _, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger8");
        var created = await RequestBookingAsync(passengerClient, tripId, 1);
        await driverClient.PostAsync($"/api/v1/bookings/{created.Id}/accept", null);

        var response = await driverClient.PostAsync($"/api/v1/bookings/{created.Id}/complete", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CompleteBooking_AfterDeparture_Succeeds()
    {
        var (driverClient, _, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger9");
        var created = await RequestBookingAsync(passengerClient, tripId, 1);
        await driverClient.PostAsync($"/api/v1/bookings/{created.Id}/accept", null);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE trips SET departure_at_utc = {DateTimeOffset.UtcNow.AddDays(-1)} WHERE id = {tripId}");
        }

        var response = await driverClient.PostAsync($"/api/v1/bookings/{created.Id}/complete", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var completed = await response.Content.ReadFromJsonAsync<BookingResponse>();
        completed!.Status.ShouldBe("Completed");
    }

    [Fact]
    public async Task GetMyBookings_OnlyReturnsCallersOwn()
    {
        var (_, _, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger10");
        await RequestBookingAsync(passengerClient, tripId, 1);

        var (otherClient, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger11");
        await RequestBookingAsync(otherClient, tripId, 1);

        var response = await passengerClient.GetAsync("/api/v1/bookings/me");

        var bookings = await response.Content.ReadFromJsonAsync<List<BookingResponse>>();
        bookings!.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetTripRequests_DriverSeesAllRequestsForTheirTrip()
    {
        var (driverClient, _, tripId) = await CreateTripAsync();
        var (passengerA, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger12");
        var (passengerB, _, _) = await factory.CreateAuthenticatedClientAsync("Passenger13");
        await RequestBookingAsync(passengerA, tripId, 1);
        await RequestBookingAsync(passengerB, tripId, 1);

        var response = await driverClient.GetAsync($"/api/v1/bookings/trips/{tripId}/requests");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var bookings = await response.Content.ReadFromJsonAsync<List<BookingResponse>>();
        bookings!.Count.ShouldBe(2);
    }

    /// <summary>
    /// The whole point of Phase 6: two passengers racing for the last seat must never both
    /// win. Fires two concurrent requests for the trip's only seat on the same segment and
    /// asserts exactly one is accepted (201) and the other is rejected (409) — proving the
    /// pg_advisory_xact_lock serializes them rather than both reading the same pre-decrement
    /// SeatsAvailable and both succeeding.
    /// </summary>
    [Fact]
    public async Task RequestBooking_TwoConcurrentRequestsForTheLastSeat_OnlyOneSucceeds()
    {
        var (_, _, tripId) = await CreateTripAsync(seats: 1);
        var (passengerA, _, _) = await factory.CreateAuthenticatedClientAsync("ConcurrentA");
        var (passengerB, _, _) = await factory.CreateAuthenticatedClientAsync("ConcurrentB");

        var requestA = passengerA.PostAsJsonAsync("/api/v1/bookings", BookJohannesburgToPolokwane(tripId, 1));
        var requestB = passengerB.PostAsJsonAsync("/api/v1/bookings", BookJohannesburgToPolokwane(tripId, 1));

        var responses = await Task.WhenAll(requestA, requestB);

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).ShouldBe(1);

        var tripResponse = await passengerA.GetAsync($"/api/v1/trips/{tripId}");
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDetailResponse>();
        trip!.Segments.Single(s => s.FromSequence == 0 && s.ToSequence == 1).SeatsAvailable.ShouldBe(0);
    }

    /// <summary>The advisory lock serializes access, not bookings — two concurrent requests for
    /// *different, non-overlapping* segments of the same trip must both still succeed rather
    /// than one spuriously failing because they happened to race on the same trip-scoped lock.</summary>
    [Fact]
    public async Task RequestBooking_ConcurrentRequestsForDifferentSegments_BothSucceed()
    {
        var (_, _, tripId) = await CreateTripAsync(seats: 1);
        var (passengerA, _, _) = await factory.CreateAuthenticatedClientAsync("SegA");
        var (passengerB, _, _) = await factory.CreateAuthenticatedClientAsync("SegB");

        var firstLegRequest = passengerA.PostAsJsonAsync("/api/v1/bookings", BookJohannesburgToPolokwane(tripId, 1));
        var secondLegRequest = passengerB.PostAsJsonAsync("/api/v1/bookings", BookPolokwaneToGiyani(tripId, 1));

        var responses = await Task.WhenAll(firstLegRequest, secondLegRequest);

        responses.ShouldAllBe(r => r.StatusCode == HttpStatusCode.Created);
    }

    private async Task<BookingResponse> RequestBookingAsync(HttpClient passengerClient, Guid tripId, int seats)
    {
        var response = await passengerClient.PostAsJsonAsync("/api/v1/bookings", BookJohannesburgToPolokwane(tripId, seats));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookingResponse>())!;
    }

    private sealed record VehicleResponse(Guid Id);

    private sealed record TripResponse(Guid Id);

    private sealed record TripSegmentResponse(int FromSequence, int ToSequence, int SeatsAvailable);

    private sealed record TripDetailResponse(Guid Id, List<TripSegmentResponse> Segments);

    private sealed record BookingResponse(
        Guid Id, string Status, int SeatsRequested, decimal TotalPrice,
        string BoardingStopName, string AlightingStopName, string? CancellationReason);
}
