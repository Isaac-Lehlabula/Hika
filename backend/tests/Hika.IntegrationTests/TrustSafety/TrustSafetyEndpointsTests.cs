using System.Net;
using System.Net.Http.Json;
using Hika.IntegrationTests.TestSupport;
using Shouldly;

namespace Hika.IntegrationTests.TrustSafety;

public class TrustSafetyEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task FileReport_AboutAUser_ReturnsOpenReport()
    {
        var (reporterClient, _, _) = await factory.CreateAuthenticatedClientAsync("Reporter1");
        var (_, reportedUserId, _) = await factory.CreateAuthenticatedClientAsync("Reported1");

        var response = await reporterClient.PostAsJsonAsync("/api/v1/trust-safety/reports", new
        {
            reportedUserId,
            reason = "Harassment",
            description = "Made me uncomfortable during the trip",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var report = await response.Content.ReadFromJsonAsync<ReportResponse>();
        report!.Status.ShouldBe("Open");
        report.ReportedUserId.ShouldBe(reportedUserId);
    }

    [Fact]
    public async Task FileReport_WithoutUserOrTrip_ReturnsBadRequest()
    {
        var (reporterClient, _, _) = await factory.CreateAuthenticatedClientAsync("Reporter2");

        var response = await reporterClient.PostAsJsonAsync("/api/v1/trust-safety/reports", new
        {
            reason = "Other",
            description = "Something happened",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BlockUser_ThenGetMyBlocks_ReturnsTheBlockedUser()
    {
        var (blockerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Blocker1");
        var (_, blockedUserId, _) = await factory.CreateAuthenticatedClientAsync("Blocked1");

        var blockResponse = await blockerClient.PostAsync($"/api/v1/trust-safety/blocks/{blockedUserId}", null);
        blockResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var listResponse = await blockerClient.GetAsync("/api/v1/trust-safety/blocks");
        var blocks = await listResponse.Content.ReadFromJsonAsync<List<BlockedUserResponse>>();
        blocks!.ShouldContain(b => b.UserId == blockedUserId);
    }

    [Fact]
    public async Task UnblockUser_RemovesFromMyBlocks()
    {
        var (blockerClient, _, _) = await factory.CreateAuthenticatedClientAsync("Blocker2");
        var (_, blockedUserId, _) = await factory.CreateAuthenticatedClientAsync("Blocked2");
        await blockerClient.PostAsync($"/api/v1/trust-safety/blocks/{blockedUserId}", null);

        var unblockResponse = await blockerClient.DeleteAsync($"/api/v1/trust-safety/blocks/{blockedUserId}");

        unblockResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var listResponse = await blockerClient.GetAsync("/api/v1/trust-safety/blocks");
        var blocks = await listResponse.Content.ReadFromJsonAsync<List<BlockedUserResponse>>();
        blocks!.ShouldNotContain(b => b.UserId == blockedUserId);
    }

    [Fact]
    public async Task CreateEmergencyContact_ThenList_ReturnsIt()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync("EmergencyUser1");

        var createResponse = await client.PostAsJsonAsync("/api/v1/users/me/emergency-contacts", new
        {
            name = "Naledi Dlamini",
            phoneNumber = "+27821234567",
            relationship = "Sister",
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var listResponse = await client.GetAsync("/api/v1/users/me/emergency-contacts");
        var contacts = await listResponse.Content.ReadFromJsonAsync<List<EmergencyContactResponse>>();
        contacts!.ShouldHaveSingleItem();
        contacts[0].Name.ShouldBe("Naledi Dlamini");
    }

    [Fact]
    public async Task UpdateEmergencyContact_ChangesFields()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync("EmergencyUser2");
        var createResponse = await client.PostAsJsonAsync("/api/v1/users/me/emergency-contacts", new
        {
            name = "Naledi Dlamini",
            phoneNumber = "+27821234567",
            relationship = "Sister",
        });
        var created = await createResponse.Content.ReadFromJsonAsync<EmergencyContactResponse>();

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/users/me/emergency-contacts/{created!.Id}", new
        {
            name = "Naledi M",
            phoneNumber = "+27829999999",
            relationship = "Mother",
        });

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<EmergencyContactResponse>();
        updated!.Name.ShouldBe("Naledi M");
        updated.Relationship.ShouldBe("Mother");
    }

    [Fact]
    public async Task DeleteEmergencyContact_RemovesIt()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync("EmergencyUser3");
        var createResponse = await client.PostAsJsonAsync("/api/v1/users/me/emergency-contacts", new
        {
            name = "Naledi Dlamini",
            phoneNumber = "+27821234567",
        });
        var created = await createResponse.Content.ReadFromJsonAsync<EmergencyContactResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/users/me/emergency-contacts/{created!.Id}");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var listResponse = await client.GetAsync("/api/v1/users/me/emergency-contacts");
        var contacts = await listResponse.Content.ReadFromJsonAsync<List<EmergencyContactResponse>>();
        contacts!.ShouldBeEmpty();
    }

    [Fact]
    public async Task EmergencyContact_NotOwner_CannotUpdateOrDelete()
    {
        var (ownerClient, _, _) = await factory.CreateAuthenticatedClientAsync("EmergencyOwner");
        var createResponse = await ownerClient.PostAsJsonAsync("/api/v1/users/me/emergency-contacts", new
        {
            name = "Naledi Dlamini",
            phoneNumber = "+27821234567",
        });
        var created = await createResponse.Content.ReadFromJsonAsync<EmergencyContactResponse>();

        var (strangerClient, _, _) = await factory.CreateAuthenticatedClientAsync("EmergencyStranger");
        var updateResponse = await strangerClient.PutAsJsonAsync($"/api/v1/users/me/emergency-contacts/{created!.Id}", new
        {
            name = "Hacked",
            phoneNumber = "+27821234567",
        });
        var deleteResponse = await strangerClient.DeleteAsync($"/api/v1/users/me/emergency-contacts/{created.Id}");

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RequestBooking_BetweenBlockedUsers_ReturnsForbidden()
    {
        var (driverClient, driverUserId, _) = await factory.CreateAuthenticatedClientAsync("BlockDriver");
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
            stops = new object[]
            {
                new { rawName = "Johannesburg", province = "Gauteng" },
                new { rawName = "Polokwane", province = "Limpopo" },
            },
        });
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripResponse>();

        var (passengerClient, _, _) = await factory.CreateAuthenticatedClientAsync("BlockPassenger");
        (await passengerClient.PostAsync($"/api/v1/trust-safety/blocks/{driverUserId}", null)).EnsureSuccessStatusCode();

        var response = await passengerClient.PostAsJsonAsync("/api/v1/bookings", new
        {
            tripId = trip!.Id,
            boardingStopSequence = 0,
            alightingStopSequence = 1,
            seatsRequested = 1,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private sealed record VehicleResponse(Guid Id);

    private sealed record TripResponse(Guid Id);

    private sealed record ReportResponse(Guid Id, Guid? ReportedUserId, Guid? ReportedTripId, string Reason, string Description, string Status);

    private sealed record BlockedUserResponse(Guid UserId, string FirstName, string LastName, string? PhotoUrl, DateTimeOffset BlockedAtUtc);

    private sealed record EmergencyContactResponse(Guid Id, string Name, string PhoneNumber, string? Relationship);
}
