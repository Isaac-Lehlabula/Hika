using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Hika.Infrastructure.Persistence;
using Hika.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Hika.IntegrationTests.Chat;

public class ChatEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly object[] JohannesburgToGiyaniStops =
    [
        new { rawName = "Johannesburg", province = "Gauteng" },
        new { rawName = "Polokwane", province = "Limpopo" },
    ];

    private async Task<(HttpClient DriverClient, HttpClient PassengerClient, Guid BookingId, Guid TripId)> CreateBookingAsync(
        [CallerMemberName] string testName = "")
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
            departureAtUtc = DateTimeOffset.UtcNow.AddDays(3),
            totalSeatsOffered = 4,
            pricePerSeat = 300m,
            stops = JohannesburgToGiyaniStops,
        });
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripResponse>();

        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync($"{testName}Passenger");
        var bookingResponse = await passengerClient.PostAsJsonAsync("/api/v1/bookings", new
        {
            tripId = trip!.Id,
            boardingStopSequence = 0,
            alightingStopSequence = 1,
            seatsRequested = 1,
        });
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>();

        return (driverClient, passengerClient, booking!.Id, trip.Id);
    }

    private async Task<(HttpClient DriverClient, HttpClient PassengerClient, Guid BookingId)> CreateAcceptedBookingAsync(
        [CallerMemberName] string testName = "")
    {
        var (driverClient, passengerClient, bookingId, _) = await CreateBookingAsync(testName);
        (await driverClient.PostAsync($"/api/v1/bookings/{bookingId}/accept", null)).EnsureSuccessStatusCode();

        return (driverClient, passengerClient, bookingId);
    }

    [Fact]
    public async Task GetConversation_BeforeAccept_ReturnsNotFound()
    {
        var (_, passengerClient, bookingId, _) = await CreateBookingAsync();

        var response = await passengerClient.GetAsync($"/api/v1/bookings/{bookingId}/conversation");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AcceptBooking_OpensAnOpenEmptyConversation()
    {
        var (driverClient, _, bookingId) = await CreateAcceptedBookingAsync();

        var response = await driverClient.GetAsync($"/api/v1/bookings/{bookingId}/conversation");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var conversation = await response.Content.ReadFromJsonAsync<ConversationResponse>();
        conversation!.IsOpen.ShouldBeTrue();
        conversation.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task SendMessage_ByPassenger_AppearsForBothSidesWithCorrectIsMine()
    {
        var (driverClient, passengerClient, bookingId) = await CreateAcceptedBookingAsync();

        var sendResponse = await passengerClient.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/conversation/messages", new { message = "Running 10 minutes late" });
        sendResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var passengerView = await (await passengerClient.GetAsync($"/api/v1/bookings/{bookingId}/conversation"))
            .Content.ReadFromJsonAsync<ConversationResponse>();
        passengerView!.Messages.Single().Body.ShouldBe("Running 10 minutes late");
        passengerView.Messages.Single().IsMine.ShouldBeTrue();

        var driverView = await (await driverClient.GetAsync($"/api/v1/bookings/{bookingId}/conversation"))
            .Content.ReadFromJsonAsync<ConversationResponse>();
        driverView!.Messages.Single().Body.ShouldBe("Running 10 minutes late");
        driverView.Messages.Single().IsMine.ShouldBeFalse();
    }

    [Fact]
    public async Task SendMessage_NotifiesTheOtherParty()
    {
        var (driverClient, passengerClient, bookingId) = await CreateAcceptedBookingAsync();

        (await passengerClient.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/conversation/messages", new { message = "See you at the stop" }))
            .EnsureSuccessStatusCode();

        var notifications = await (await driverClient.GetAsync("/api/v1/notifications/me?page=1&pageSize=20"))
            .Content.ReadFromJsonAsync<PagedNotificationsResponse>();
        notifications!.Items.ShouldContain(n => n.Type == "NewChatMessage");
    }

    [Fact]
    public async Task SendMessage_EmptyBody_ReturnsBadRequest()
    {
        var (_, passengerClient, bookingId) = await CreateAcceptedBookingAsync();

        var response = await passengerClient.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/conversation/messages", new { message = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Conversation_NonParticipant_CannotAccessOrSend()
    {
        var (_, _, bookingId) = await CreateAcceptedBookingAsync();
        var (strangerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Stranger");

        var getResponse = await strangerClient.GetAsync($"/api/v1/bookings/{bookingId}/conversation");
        var postResponse = await strangerClient.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/conversation/messages", new { message = "Hi" });

        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        postResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteBooking_ClosesTheConversationAndBlocksFurtherMessages()
    {
        var (driverClient, passengerClient, bookingId) = await CreateAcceptedBookingAsync();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE trips SET departure_at_utc = {DateTimeOffset.UtcNow.AddDays(-1)}
                 WHERE id = (SELECT trip_id FROM bookings WHERE id = {bookingId})
                 """);
        }

        (await driverClient.PostAsync($"/api/v1/bookings/{bookingId}/complete", null)).EnsureSuccessStatusCode();

        var conversation = await (await driverClient.GetAsync($"/api/v1/bookings/{bookingId}/conversation"))
            .Content.ReadFromJsonAsync<ConversationResponse>();
        conversation!.IsOpen.ShouldBeFalse();

        var sendAfterClose = await passengerClient.PostAsJsonAsync(
            $"/api/v1/bookings/{bookingId}/conversation/messages", new { message = "Thanks for the lift!" });
        sendAfterClose.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CancelBooking_AfterAccept_ClosesTheConversation()
    {
        var (_, passengerClient, bookingId) = await CreateAcceptedBookingAsync();

        (await passengerClient.PostAsJsonAsync($"/api/v1/bookings/{bookingId}/cancel", new { reason = "Change of plans" }))
            .EnsureSuccessStatusCode();

        var conversation = await (await passengerClient.GetAsync($"/api/v1/bookings/{bookingId}/conversation"))
            .Content.ReadFromJsonAsync<ConversationResponse>();
        conversation!.IsOpen.ShouldBeFalse();
    }

    private sealed record VehicleResponse(Guid Id);

    private sealed record TripResponse(Guid Id);

    private sealed record BookingResponse(Guid Id, string Status);

    private sealed record ChatMessageItem(Guid Id, Guid SenderUserId, bool IsMine, string Body, DateTimeOffset SentAtUtc);

    private sealed record ConversationResponse(Guid Id, Guid BookingId, bool IsOpen, List<ChatMessageItem> Messages);

    private sealed record NotificationItem(Guid Id, string Type, string Message, string Status);

    private sealed record PagedNotificationsResponse(List<NotificationItem> Items, int Page, int PageSize, int TotalCount);
}
