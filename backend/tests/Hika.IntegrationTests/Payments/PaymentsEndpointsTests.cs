using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Hika.IntegrationTests.TestSupport;
using Shouldly;

namespace Hika.IntegrationTests.Payments;

public class PaymentsEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly object[] JohannesburgToGiyaniStops =
    [
        new { rawName = "Johannesburg", province = "Gauteng" },
        new { rawName = "Polokwane", province = "Limpopo" },
        new { rawName = "Giyani", province = "Limpopo" },
    ];

    private async Task<(HttpClient DriverClient, Guid TripId)> CreateTripAsync([CallerMemberName] string testName = "")
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
        var vehicle = await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>();

        var tripResponse = await client.PostAsJsonAsync("/api/v1/trips", new
        {
            vehicleId = vehicle!.Id,
            departureAtUtc = DateTimeOffset.UtcNow.AddDays(3),
            totalSeatsOffered = 4,
            pricePerSeat = 400m,
            stops = JohannesburgToGiyaniStops,
        });
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripResponse>();

        return (client, trip!.Id);
    }

    private async Task<BookingResponse> RequestAndAcceptBookingAsync(
        HttpClient driverClient, Guid tripId, HttpClient passengerClient, int seats = 2)
    {
        var bookingResponse = await passengerClient.PostAsJsonAsync("/api/v1/bookings", new
        {
            tripId,
            boardingStopSequence = 0,
            alightingStopSequence = 1,
            seatsRequested = seats,
        });
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>();

        var acceptResponse = await driverClient.PostAsync($"/api/v1/bookings/{booking!.Id}/accept", null);
        acceptResponse.EnsureSuccessStatusCode();

        return booking;
    }

    [Fact]
    public async Task AcceptingABooking_CapturesAPaymentWithCorrectSplit()
    {
        var (driverClient, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("PaymentPassenger1");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient, seats: 2);

        var response = await passengerClient.GetAsync($"/api/v1/bookings/{booking.Id}/payment");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        payment!.Status.ShouldBe("Succeeded");
        payment.Amount.ShouldBe(800m);
        payment.PlatformFee.ShouldBe(120m);
        payment.DriverPayoutAmount.ShouldBe(680m);
        payment.Provider.ShouldBe("Mock");
        payment.ProviderReference.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPayment_DriverCanAlsoView()
    {
        var (driverClient, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("PaymentPassenger2");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);

        var response = await driverClient.GetAsync($"/api/v1/bookings/{booking.Id}/payment");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPayment_StrangerCannotView()
    {
        var (driverClient, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("PaymentPassenger3");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);

        var (strangerClient, _, _) = await factory.CreateAuthenticatedClientAsync("PaymentStranger");
        var response = await strangerClient.GetAsync($"/api/v1/bookings/{booking.Id}/payment");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPayment_BeforeAccept_ReturnsNotFound()
    {
        var (driverClient, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("PaymentPassenger4");

        var bookingResponse = await passengerClient.PostAsJsonAsync("/api/v1/bookings", new
        {
            tripId,
            boardingStopSequence = 0,
            alightingStopSequence = 1,
            seatsRequested = 1,
        });
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>();

        var response = await passengerClient.GetAsync($"/api/v1/bookings/{booking!.Id}/payment");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RefundPayment_ByDriver_MarksPaymentRefunded()
    {
        var (driverClient, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("PaymentPassenger5");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);

        var response = await driverClient.PostAsJsonAsync($"/api/v1/bookings/{booking.Id}/refund", new { reason = "Trip cancelled by driver" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var refunded = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        refunded!.Status.ShouldBe("Refunded");
    }

    [Fact]
    public async Task RefundPayment_ByPassenger_ReturnsNotFound()
    {
        var (driverClient, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("PaymentPassenger6");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);

        var response = await passengerClient.PostAsJsonAsync($"/api/v1/bookings/{booking.Id}/refund", new { reason = "Change of plans" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RefundPayment_AlreadyRefunded_ReturnsConflict()
    {
        var (driverClient, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("PaymentPassenger7");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);
        (await driverClient.PostAsJsonAsync($"/api/v1/bookings/{booking.Id}/refund", new { reason = "First refund" })).EnsureSuccessStatusCode();

        var response = await driverClient.PostAsJsonAsync($"/api/v1/bookings/{booking.Id}/refund", new { reason = "Second refund" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private sealed record VehicleResponse(Guid Id);

    private sealed record TripResponse(Guid Id);

    private sealed record BookingResponse(Guid Id);

    private sealed record PaymentResponse(
        Guid Id, Guid BookingId, decimal Amount, decimal PlatformFee, decimal DriverPayoutAmount,
        string Provider, string? ProviderReference, string Status);
}
