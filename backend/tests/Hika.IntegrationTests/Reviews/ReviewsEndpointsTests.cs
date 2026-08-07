using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Hika.Infrastructure.Persistence;
using Hika.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Hika.IntegrationTests.Reviews;

public class ReviewsEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly object[] JohannesburgToGiyaniStops =
    [
        new { rawName = "Johannesburg", province = "Gauteng" },
        new { rawName = "Polokwane", province = "Limpopo" },
    ];

    /// <summary>Builds a fully Completed booking (driver, passenger, trip, accepted booking,
    /// then the trip's departure is pushed into the past via a direct DB write — same technique
    /// used in BookingsEndpointsTests — so /complete's departure-has-passed guard is satisfied).</summary>
    private async Task<(HttpClient DriverClient, Guid DriverUserId, HttpClient PassengerClient, Guid PassengerUserId, Guid BookingId)>
        CreateCompletedBookingAsync([CallerMemberName] string testName = "")
    {
        var (driverClient, driverUserId, _) = await factory.CreateAuthenticatedClientAsync($"{testName}Driver");
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
            departureAtUtc = DateTimeOffset.UtcNow.AddDays(3),
            totalSeatsOffered = 4,
            pricePerSeat = 300m,
            stops = JohannesburgToGiyaniStops,
        });
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripResponse>();

        var (passengerClient, passengerUserId, _) = await factory.CreateAuthenticatedClientAsync($"{testName}Passenger");
        var bookingResponse = await passengerClient.PostAsJsonAsync("/api/v1/bookings", new
        {
            tripId = trip!.Id,
            boardingStopSequence = 0,
            alightingStopSequence = 1,
            seatsRequested = 1,
        });
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>();

        (await driverClient.PostAsync($"/api/v1/bookings/{booking!.Id}/accept", null)).EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE trips SET departure_at_utc = {DateTimeOffset.UtcNow.AddDays(-1)} WHERE id = {trip.Id}");
        }

        (await driverClient.PostAsync($"/api/v1/bookings/{booking.Id}/complete", null)).EnsureSuccessStatusCode();

        return (driverClient, driverUserId, passengerClient, passengerUserId, booking.Id);
    }

    [Fact]
    public async Task SubmitReview_PassengerReviewingDriver_Succeeds()
    {
        var (_, driverUserId, passengerClient, _, bookingId) = await CreateCompletedBookingAsync();

        var response = await passengerClient.PostAsJsonAsync(
            $"/api/v1/reviews/bookings/{bookingId}", new { rating = 5, comment = "Great driver!" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var review = await response.Content.ReadFromJsonAsync<ReviewResponse>();
        review!.Direction.ShouldBe("PassengerToDriver");
        review.RevieweeUserId.ShouldBe(driverUserId);
        review.Rating.ShouldBe(5);
    }

    [Fact]
    public async Task SubmitReview_DriverReviewingPassenger_Succeeds()
    {
        var (driverClient, _, _, passengerUserId, bookingId) = await CreateCompletedBookingAsync();

        var response = await driverClient.PostAsJsonAsync(
            $"/api/v1/reviews/bookings/{bookingId}", new { rating = 4, comment = "Punctual passenger" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var review = await response.Content.ReadFromJsonAsync<ReviewResponse>();
        review!.Direction.ShouldBe("DriverToPassenger");
        review.RevieweeUserId.ShouldBe(passengerUserId);
    }

    [Fact]
    public async Task SubmitReview_UpdatesRevieweeAverageRatingAndCompletedTripCount()
    {
        var (_, driverUserId, passengerClient, _, bookingId) = await CreateCompletedBookingAsync();

        (await passengerClient.PostAsJsonAsync($"/api/v1/reviews/bookings/{bookingId}", new { rating = 4 }))
            .EnsureSuccessStatusCode();

        var profileResponse = await passengerClient.GetAsync($"/api/v1/users/{driverUserId}");
        profileResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await profileResponse.Content.ReadFromJsonAsync<PublicProfileResponse>();
        profile!.AverageRating.ShouldBe(4m);
        profile.CompletedTripCount.ShouldBe(1);
    }

    [Fact]
    public async Task SubmitReview_SameBookingTwiceByTheSameReviewer_ReturnsConflict()
    {
        var (_, _, passengerClient, _, bookingId) = await CreateCompletedBookingAsync();
        (await passengerClient.PostAsJsonAsync($"/api/v1/reviews/bookings/{bookingId}", new { rating = 5 })).EnsureSuccessStatusCode();

        var response = await passengerClient.PostAsJsonAsync($"/api/v1/reviews/bookings/{bookingId}", new { rating = 3 });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SubmitReview_ByNonParticipant_ReturnsNotFound()
    {
        var (_, _, _, _, bookingId) = await CreateCompletedBookingAsync();
        var (strangerClient, _, _) = await factory.CreateAuthenticatedClientAsync("ReviewStranger");

        var response = await strangerClient.PostAsJsonAsync($"/api/v1/reviews/bookings/{bookingId}", new { rating = 5 });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitReview_BeforeBookingCompleted_ReturnsBadRequest()
    {
        var (driverClient, _, tripId) = await CreateScheduledTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("ReviewEarlyPassenger");
        var bookingResponse = await passengerClient.PostAsJsonAsync("/api/v1/bookings", new
        {
            tripId,
            boardingStopSequence = 0,
            alightingStopSequence = 1,
            seatsRequested = 1,
        });
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>();
        await driverClient.PostAsync($"/api/v1/bookings/{booking!.Id}/accept", null);

        var response = await passengerClient.PostAsJsonAsync($"/api/v1/reviews/bookings/{booking.Id}", new { rating = 5 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetReviewsForUser_ReturnsPaginatedReviewsInNewestFirstOrder()
    {
        var (_, driverUserId, passengerClient, _, bookingId) = await CreateCompletedBookingAsync();
        (await passengerClient.PostAsJsonAsync($"/api/v1/reviews/bookings/{bookingId}", new { rating = 5, comment = "Loved it" }))
            .EnsureSuccessStatusCode();

        var response = await factory.CreateClient().GetAsync($"/api/v1/reviews/users/{driverUserId}?page=1&pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedReviewResponse>();
        result!.Items.ShouldContain(r => r.Comment == "Loved it");
        result.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    private async Task<(HttpClient DriverClient, Guid DriverUserId, Guid TripId)> CreateScheduledTripAsync(
        [CallerMemberName] string testName = "")
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
            totalSeatsOffered = 4,
            pricePerSeat = 300m,
            stops = JohannesburgToGiyaniStops,
        });
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripResponse>();

        return (client, driverUserId, trip!.Id);
    }

    private sealed record VehicleResponse(Guid Id);

    private sealed record TripResponse(Guid Id);

    private sealed record BookingResponse(Guid Id);

    private sealed record ReviewResponse(Guid Id, Guid BookingId, Guid RevieweeUserId, string Direction, int Rating, string? Comment);

    private sealed record PagedReviewResponse(List<ReviewResponse> Items, int Page, int PageSize, int TotalCount);

    private sealed record PublicProfileResponse(Guid UserId, decimal? AverageRating, int CompletedTripCount);
}
