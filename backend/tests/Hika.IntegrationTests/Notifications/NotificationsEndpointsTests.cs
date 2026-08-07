using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Hika.IntegrationTests.TestSupport;
using Shouldly;

namespace Hika.IntegrationTests.Notifications;

public class NotificationsEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly object[] JohannesburgToGiyaniStops =
    [
        new { rawName = "Johannesburg", province = "Gauteng" },
        new { rawName = "Polokwane", province = "Limpopo" },
    ];

    private async Task<(HttpClient DriverClient, HttpClient PassengerClient, Guid BookingId)> RequestBookingAsync(
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

        return (driverClient, passengerClient, booking!.Id);
    }

    [Fact]
    public async Task RequestingABooking_NotifiesTheDriver()
    {
        var (driverClient, _, _) = await RequestBookingAsync();

        var response = await driverClient.GetAsync("/api/v1/notifications/me?page=1&pageSize=20");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedNotificationsResponse>();
        result!.Items.ShouldContain(n => n.Type == "BookingRequested");
    }

    [Fact]
    public async Task AcceptingABooking_NotifiesThePassengerOfAcceptanceAndPayment()
    {
        var (driverClient, passengerClient, bookingId) = await RequestBookingAsync();
        (await driverClient.PostAsync($"/api/v1/bookings/{bookingId}/accept", null)).EnsureSuccessStatusCode();

        var response = await passengerClient.GetAsync("/api/v1/notifications/me?page=1&pageSize=20");

        var result = await response.Content.ReadFromJsonAsync<PagedNotificationsResponse>();
        result!.Items.ShouldContain(n => n.Type == "BookingAccepted");
        result.Items.ShouldContain(n => n.Type == "PaymentSucceeded");
    }

    [Fact]
    public async Task DecliningABooking_NotifiesThePassenger()
    {
        var (driverClient, passengerClient, bookingId) = await RequestBookingAsync();
        (await driverClient.PostAsync($"/api/v1/bookings/{bookingId}/decline", null)).EnsureSuccessStatusCode();

        var response = await passengerClient.GetAsync("/api/v1/notifications/me?page=1&pageSize=20");

        var result = await response.Content.ReadFromJsonAsync<PagedNotificationsResponse>();
        result!.Items.ShouldContain(n => n.Type == "BookingDeclined");
    }

    [Fact]
    public async Task MarkRead_OwnNotification_Succeeds()
    {
        var (driverClient, _, _) = await RequestBookingAsync();
        var listResponse = await driverClient.GetAsync("/api/v1/notifications/me?page=1&pageSize=20");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedNotificationsResponse>();
        var notification = list!.Items.Single(n => n.Type == "BookingRequested");

        var response = await driverClient.PostAsync($"/api/v1/notifications/me/{notification.Id}/read", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var refetched = await driverClient.GetAsync("/api/v1/notifications/me?page=1&pageSize=20");
        var refetchedList = await refetched.Content.ReadFromJsonAsync<PagedNotificationsResponse>();
        refetchedList!.Items.Single(n => n.Id == notification.Id).Status.ShouldBe("Read");
    }

    [Fact]
    public async Task MarkRead_NotOwnNotification_ReturnsNotFound()
    {
        var (driverClient, _, _) = await RequestBookingAsync();
        var listResponse = await driverClient.GetAsync("/api/v1/notifications/me?page=1&pageSize=20");
        var list = await listResponse.Content.ReadFromJsonAsync<PagedNotificationsResponse>();
        var notification = list!.Items.Single(n => n.Type == "BookingRequested");

        var (strangerClient, _, _) = await factory.CreateAuthenticatedClientAsync("NotificationStranger");
        var response = await strangerClient.PostAsync($"/api/v1/notifications/me/{notification.Id}/read", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private sealed record VehicleResponse(Guid Id);

    private sealed record TripResponse(Guid Id);

    private sealed record BookingResponse(Guid Id);

    private sealed record NotificationItem(Guid Id, string Type, string Message, string Status);

    private sealed record PagedNotificationsResponse(List<NotificationItem> Items, int Page, int PageSize, int TotalCount);
}
