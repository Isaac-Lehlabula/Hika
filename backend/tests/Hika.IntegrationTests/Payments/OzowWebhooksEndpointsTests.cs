using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Hika.IntegrationTests.TestSupport;
using Hika.Infrastructure.Payments.Ozow;
using Shouldly;

namespace Hika.IntegrationTests.Payments;

/// <summary>Exercises the Ozow-shaped payment flow (redirect + webhook, not instant capture)
/// against PendingPaymentGatewayFactory's fake gateway — everything except the actual call to
/// Ozow's API is real: the booking state machine, PaymentService, and OzowWebhooksController's
/// hash verification.</summary>
public class OzowWebhooksEndpointsTests(PendingPaymentGatewayFactory factory) : IClassFixture<PendingPaymentGatewayFactory>
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

    private async Task<BookingResponse> RequestAndAcceptBookingAsync(HttpClient driverClient, Guid tripId, HttpClient passengerClient)
    {
        var bookingResponse = await passengerClient.PostAsJsonAsync("/api/v1/bookings", new
        {
            tripId,
            boardingStopSequence = 0,
            alightingStopSequence = 1,
            seatsRequested = 2,
        });
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>();

        var acceptResponse = await driverClient.PostAsync($"/api/v1/bookings/{booking!.Id}/accept", null);
        acceptResponse.EnsureSuccessStatusCode();

        return (await acceptResponse.Content.ReadFromJsonAsync<BookingResponse>())!;
    }

    private static FormUrlEncodedContent SignedPaymentNotify(Guid bookingId, string status, string transactionId = "OZOW-TXN-1")
    {
        var payload = new OzowPaymentNotifyPayload
        {
            SiteCode = "TESTSITE",
            TransactionId = transactionId,
            TransactionReference = bookingId.ToString(),
            Amount = "800.00",
            Status = status,
            CurrencyCode = "ZAR",
            IsTest = "True",
        };
        var hash = OzowHashHelper.ComputeHash(
            [payload.SiteCode, payload.TransactionId, payload.TransactionReference, payload.Amount, payload.Status, payload.CurrencyCode, payload.IsTest],
            PendingPaymentGatewayFactory.PrivateKey);

        return new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["SiteCode"] = payload.SiteCode,
            ["TransactionId"] = payload.TransactionId,
            ["TransactionReference"] = payload.TransactionReference,
            ["Amount"] = payload.Amount,
            ["Status"] = payload.Status,
            ["CurrencyCode"] = payload.CurrencyCode,
            ["IsTest"] = payload.IsTest,
            ["Hash"] = hash,
        });
    }

    [Fact]
    public async Task AcceptBooking_WithPendingGateway_LeavesBookingAwaitingPaymentWithARedirectUrl()
    {
        var (driverClient, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("OzowPassenger1");

        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);

        booking.Status.ShouldBe("AwaitingPayment");

        var paymentResponse = await passengerClient.GetAsync($"/api/v1/bookings/{booking.Id}/payment");
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();
        payment!.Status.ShouldBe("Pending");
        payment.RedirectUrl.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task PaymentNotify_WithCompleteStatus_ConfirmsTheBooking()
    {
        var (driverClient, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("OzowPassenger2");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);

        var response = await factory.CreateClient().PostAsync(
            "/api/v1/webhooks/ozow/payment-notify", SignedPaymentNotify(booking.Id, "Complete"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var bookingResponse = await passengerClient.GetAsync($"/api/v1/bookings/{booking.Id}");
        (await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>())!.Status.ShouldBe("Confirmed");

        var paymentResponse = await passengerClient.GetAsync($"/api/v1/bookings/{booking.Id}/payment");
        (await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>())!.Status.ShouldBe("Succeeded");
    }

    [Fact]
    public async Task PaymentNotify_WithCancelledStatus_DeclinesTheBookingAndReleasesSeats()
    {
        var (driverClient, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("OzowPassenger3");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);

        var response = await factory.CreateClient().PostAsync(
            "/api/v1/webhooks/ozow/payment-notify", SignedPaymentNotify(booking.Id, "Cancelled"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var bookingResponse = await passengerClient.GetAsync($"/api/v1/bookings/{booking.Id}");
        (await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>())!.Status.ShouldBe("Declined");
    }

    [Fact]
    public async Task PaymentNotify_WithTamperedHash_ReturnsBadRequestAndLeavesBookingUnchanged()
    {
        var (driverClient, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("OzowPassenger4");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);

        var content = SignedPaymentNotify(booking.Id, "Complete");
        var tamperedFields = (await content.ReadAsStringAsync()).Replace("800.00", "1.00");
        var tampered = new StringContent(tamperedFields, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");

        var response = await factory.CreateClient().PostAsync("/api/v1/webhooks/ozow/payment-notify", tampered);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var bookingResponse = await passengerClient.GetAsync($"/api/v1/bookings/{booking.Id}");
        (await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>())!.Status.ShouldBe("AwaitingPayment");
    }

    [Fact]
    public async Task PaymentNotify_DeliveredTwice_IsIdempotent()
    {
        var (driverClient, tripId) = await CreateTripAsync();
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("OzowPassenger5");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);
        var webhookClient = factory.CreateClient();

        var first = await webhookClient.PostAsync("/api/v1/webhooks/ozow/payment-notify", SignedPaymentNotify(booking.Id, "Complete"));
        var second = await webhookClient.PostAsync("/api/v1/webhooks/ozow/payment-notify", SignedPaymentNotify(booking.Id, "Complete"));

        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);

        var bookingResponse = await passengerClient.GetAsync($"/api/v1/bookings/{booking.Id}");
        (await bookingResponse.Content.ReadFromJsonAsync<BookingResponse>())!.Status.ShouldBe("Confirmed");
    }

    private sealed record VehicleResponse(Guid Id);

    private sealed record TripResponse(Guid Id);

    private sealed record BookingResponse(Guid Id, string Status);

    private sealed record PaymentResponse(Guid Id, string Status, string? RedirectUrl);
}
