using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Hika.IntegrationTests.TestSupport;
using Shouldly;

namespace Hika.IntegrationTests.Admin;

public class AdminEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly object[] JohannesburgToGiyaniStops =
    [
        new { rawName = "Johannesburg", province = "Gauteng" },
        new { rawName = "Polokwane", province = "Limpopo" },
        new { rawName = "Giyani", province = "Limpopo" },
    ];

    private async Task<HttpClient> CreateAdminClientAsync([CallerMemberName] string testName = "")
    {
        var (client, userId, _) = await factory.CreateAuthenticatedClientAsync(testName);
        await factory.PromoteToAdminAsync(userId);
        return client;
    }

    private async Task<(HttpClient DriverClient, Guid DriverUserId, Guid TripId)> CreateTripAsync([CallerMemberName] string testName = "")
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
            pricePerSeat = 400m,
            stops = JohannesburgToGiyaniStops,
        });
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripResponse>();

        return (client, driverUserId, trip!.Id);
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

        (await driverClient.PostAsync($"/api/v1/bookings/{booking!.Id}/accept", null)).EnsureSuccessStatusCode();

        return booking;
    }

    [Fact]
    public async Task AdminEndpoint_CalledByNonAdmin_ReturnsForbidden()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/admin/users");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoint_CalledByAdmin_ReturnsOk()
    {
        var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.GetAsync("/api/v1/admin/users");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SuspendUser_ThenUnsuspend_RoundTrips()
    {
        var adminClient = await CreateAdminClientAsync();
        var (_, targetUserId, _) = await factory.CreateAuthenticatedClientAsync("SuspendTarget");

        var suspendResponse = await adminClient.PostAsJsonAsync($"/api/v1/admin/users/{targetUserId}/suspend", new { reason = "Repeated no-shows" });
        suspendResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var suspended = await suspendResponse.Content.ReadFromJsonAsync<AdminUserResponse>();
        suspended!.IsSuspended.ShouldBeTrue();
        suspended.SuspensionReason.ShouldBe("Repeated no-shows");

        var unsuspendResponse = await adminClient.PostAsync($"/api/v1/admin/users/{targetUserId}/unsuspend", null);
        var unsuspended = await unsuspendResponse.Content.ReadFromJsonAsync<AdminUserResponse>();
        unsuspended!.IsSuspended.ShouldBeFalse();
    }

    [Fact]
    public async Task ApproveVerification_MarksDriverVerified()
    {
        var adminClient = await CreateAdminClientAsync();
        var (driverClient, driverUserId, _) = await CreateTripAsync("VerifyDriver");

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("fake-license-bytes"u8.ToArray());
        fileContent.Headers.ContentType = new("image/jpeg");
        content.Add(fileContent, "file", "license.jpg");
        (await driverClient.PostAsync("/api/v1/drivers/me/driver-profile/verification-documents", content)).EnsureSuccessStatusCode();

        var queueResponse = await adminClient.GetAsync("/api/v1/admin/verifications");
        var queue = await queueResponse.Content.ReadFromJsonAsync<PagedResult<AdminVerificationResponse>>();
        var verification = queue!.Items.Single(v => v.SubjectId == driverUserId);

        var approveResponse = await adminClient.PostAsync($"/api/v1/admin/verifications/{verification.Id}/approve", null);

        approveResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var approved = await approveResponse.Content.ReadFromJsonAsync<AdminVerificationResponse>();
        approved!.Status.ShouldBe("Verified");

        var driverProfileResponse = await driverClient.GetAsync("/api/v1/drivers/me/driver-profile");
        var driverProfile = await driverProfileResponse.Content.ReadFromJsonAsync<DriverProfileResponse>();
        driverProfile!.IsVerifiedDriver.ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveTrip_CancelsIt()
    {
        var adminClient = await CreateAdminClientAsync();
        var (_, _, tripId) = await CreateTripAsync("RemoveTripDriver");

        var response = await adminClient.PostAsJsonAsync($"/api/v1/admin/trips/{tripId}/remove", new { reason = "Reported as unsafe" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var trip = await response.Content.ReadFromJsonAsync<AdminTripResponse>();
        trip!.Status.ShouldBe("Cancelled");
    }

    [Fact]
    public async Task GetBookings_IncludesARecentlyAcceptedBooking()
    {
        var adminClient = await CreateAdminClientAsync();
        var (driverClient, _, tripId) = await CreateTripAsync("AdminBookingsDriver");
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("AdminBookingsPassenger");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);

        var response = await adminClient.GetAsync("/api/v1/admin/bookings");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AdminBookingResponse>>();
        result!.Items.ShouldContain(b => b.Id == booking.Id && b.Status == "Confirmed");
    }

    [Fact]
    public async Task AdminRefund_MarksPaymentRefunded()
    {
        var adminClient = await CreateAdminClientAsync();
        var (driverClient, _, tripId) = await CreateTripAsync("AdminRefundDriver");
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("AdminRefundPassenger");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/v1/admin/payments/bookings/{booking.Id}/refund", new { reason = "Duplicate charge" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        payment!.Status.ShouldBe("Refunded");
    }

    [Fact]
    public async Task UpdatePlatformFee_AffectsSubsequentPaymentCapture()
    {
        var adminClient = await CreateAdminClientAsync();

        (await adminClient.PutAsJsonAsync("/api/v1/admin/platform-fees", new { rate = 0.2m })).EnsureSuccessStatusCode();

        var (driverClient, _, tripId) = await CreateTripAsync("FeeChangeDriver");
        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("FeeChangePassenger");
        var booking = await RequestAndAcceptBookingAsync(driverClient, tripId, passengerClient);

        var paymentResponse = await passengerClient.GetAsync($"/api/v1/bookings/{booking.Id}/payment");
        var payment = await paymentResponse.Content.ReadFromJsonAsync<PaymentResponse>();

        // 2 seats * R400 = R800 fare; 20% platform fee = R160.
        payment!.PlatformFee.ShouldBe(160m);
        payment.DriverPayoutAmount.ShouldBe(640m);
    }

    [Fact]
    public async Task ResolveReport_SetsStatusToResolved()
    {
        var adminClient = await CreateAdminClientAsync();
        var (reporterClient, _, _) = await factory.CreateAuthenticatedClientAsync("AdminReportReporter");
        var (_, reportedUserId, _) = await factory.CreateAuthenticatedClientAsync("AdminReportReported");
        var fileResponse = await reporterClient.PostAsJsonAsync("/api/v1/trust-safety/reports", new
        {
            reportedUserId,
            reason = "Harassment",
            description = "Made me uncomfortable",
        });
        var report = await fileResponse.Content.ReadFromJsonAsync<AdminReportResponse>();

        var response = await adminClient.PostAsync($"/api/v1/admin/reports/{report!.Id}/resolve", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var resolved = await response.Content.ReadFromJsonAsync<AdminReportResponse>();
        resolved!.Status.ShouldBe("Resolved");
    }

    [Fact]
    public async Task GetAnalyticsOverview_ReturnsNonNegativeCounts()
    {
        var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.GetAsync("/api/v1/admin/analytics/overview");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var overview = await response.Content.ReadFromJsonAsync<AnalyticsOverviewResponse>();
        overview!.TotalUsers.ShouldBeGreaterThan(0);
        overview.TotalTrips.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SuspendUser_IsRecordedInTheAuditLog()
    {
        var adminClient = await CreateAdminClientAsync();
        var (_, targetUserId, _) = await factory.CreateAuthenticatedClientAsync("AuditTarget");

        (await adminClient.PostAsJsonAsync($"/api/v1/admin/users/{targetUserId}/suspend", new { reason = "Testing audit trail" }))
            .EnsureSuccessStatusCode();

        var response = await adminClient.GetAsync("/api/v1/admin/audit-logs");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var logs = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogResponse>>();
        logs!.Items.ShouldContain(l => l.Action == "SuspendUser" && l.EntityId == targetUserId);
    }

    private sealed record VehicleResponse(Guid Id);

    private sealed record TripResponse(Guid Id);

    private sealed record BookingResponse(Guid Id);

    private sealed record PaymentResponse(Guid Id, decimal Amount, decimal PlatformFee, decimal DriverPayoutAmount, string Status);

    private sealed record DriverProfileResponse(Guid UserId, string LicenseNumber, bool IsVerifiedDriver, string VerificationStatus);

    private sealed record AdminUserResponse(Guid UserId, bool IsSuspended, string? SuspensionReason);

    private sealed record AdminVerificationResponse(Guid Id, Guid SubjectId, string Status);

    private sealed record AdminTripResponse(Guid Id, string Status);

    private sealed record AdminBookingResponse(Guid Id, string Status);

    private sealed record AdminReportResponse(Guid Id, string Status);

    private sealed record AnalyticsOverviewResponse(int TotalUsers, int TotalTrips);

    private sealed record AuditLogResponse(Guid Id, Guid? EntityId, string Action);

    private sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
}
