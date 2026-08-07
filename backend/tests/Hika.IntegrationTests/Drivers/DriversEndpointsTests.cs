using System.Net;
using System.Net.Http.Json;
using Hika.IntegrationTests.TestSupport;
using Shouldly;

namespace Hika.IntegrationTests.Drivers;

public class DriversEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
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

    [Fact]
    public async Task CreateDriverProfile_ThenGet_ReturnsIt()
    {
        var (client, userId, _) = await factory.CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/drivers/me/driver-profile", new
        {
            licenseNumber = "1234567890123",
            licenseExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
        });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var getResponse = await client.GetAsync("/api/v1/drivers/me/driver-profile");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var profile = await getResponse.Content.ReadFromJsonAsync<DriverProfileResponse>();
        profile!.UserId.ShouldBe(userId);
        profile.VerificationStatus.ShouldBe("NotSubmitted");
    }

    [Fact]
    public async Task GetDriverProfile_WithoutOne_ReturnsNotFound()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v1/drivers/me/driver-profile");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateVehicle_WithoutDriverProfile_ReturnsBadRequest()
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/drivers/me/vehicles", ValidVehicleBody());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<HttpClient> CreateClientWithDriverProfileAsync([System.Runtime.CompilerServices.CallerMemberName] string testName = "")
    {
        var (client, _, _) = await factory.CreateAuthenticatedClientAsync(testName);
        (await client.PostAsJsonAsync("/api/v1/drivers/me/driver-profile", new
        {
            licenseNumber = "1234567890123",
            licenseExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
        })).EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task CreateVehicle_WithDriverProfile_Succeeds()
    {
        var client = await CreateClientWithDriverProfileAsync();

        var response = await client.PostAsJsonAsync("/api/v1/drivers/me/vehicles", ValidVehicleBody());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var vehicle = await response.Content.ReadFromJsonAsync<VehicleResponse>();
        vehicle!.Make.ShouldBe("Toyota");
        vehicle.IsVerified.ShouldBeFalse();
    }

    [Fact]
    public async Task ListVehicles_OnlyReturnsCallersOwn()
    {
        var client = await CreateClientWithDriverProfileAsync();
        await client.PostAsJsonAsync("/api/v1/drivers/me/vehicles", ValidVehicleBody());
        await client.PostAsJsonAsync("/api/v1/drivers/me/vehicles", ValidVehicleBody());

        var otherClient = await CreateClientWithDriverProfileAsync("OtherDriver");
        await otherClient.PostAsJsonAsync("/api/v1/drivers/me/vehicles", ValidVehicleBody());

        var response = await client.GetAsync("/api/v1/drivers/me/vehicles");
        var vehicles = await response.Content.ReadFromJsonAsync<List<VehicleResponse>>();

        vehicles!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetVehicle_BelongingToAnotherDriver_ReturnsNotFound()
    {
        var ownerClient = await CreateClientWithDriverProfileAsync();
        var createResponse = await ownerClient.PostAsJsonAsync("/api/v1/drivers/me/vehicles", ValidVehicleBody());
        var vehicle = await createResponse.Content.ReadFromJsonAsync<VehicleResponse>();

        var otherClient = await CreateClientWithDriverProfileAsync("OtherDriver2");
        var response = await otherClient.GetAsync($"/api/v1/drivers/me/vehicles/{vehicle!.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateVehicle_ChangesFields()
    {
        var client = await CreateClientWithDriverProfileAsync();
        var createResponse = await client.PostAsJsonAsync("/api/v1/drivers/me/vehicles", ValidVehicleBody());
        var vehicle = await createResponse.Content.ReadFromJsonAsync<VehicleResponse>();

        var updateBody = ValidVehicleBody();
        var response = await client.PutAsJsonAsync($"/api/v1/drivers/me/vehicles/{vehicle!.Id}", new
        {
            make = "Honda",
            model = "Ballade",
            year = 2019,
            color = "Silver",
            registrationNumber = vehicle.RegistrationNumber,
            seatCapacity = 5,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<VehicleResponse>();
        updated!.Make.ShouldBe("Honda");
        updated.SeatCapacity.ShouldBe(5);
    }

    [Fact]
    public async Task DeleteVehicle_RemovesIt()
    {
        var client = await CreateClientWithDriverProfileAsync();
        var createResponse = await client.PostAsJsonAsync("/api/v1/drivers/me/vehicles", ValidVehicleBody());
        var vehicle = await createResponse.Content.ReadFromJsonAsync<VehicleResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/v1/drivers/me/vehicles/{vehicle!.Id}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/v1/drivers/me/vehicles/{vehicle.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UploadVehiclePhoto_TwoPrimaryPhotos_SecondWinsAndUrlIsFetchable()
    {
        var client = await CreateClientWithDriverProfileAsync();
        var createResponse = await client.PostAsJsonAsync("/api/v1/drivers/me/vehicles", ValidVehicleBody());
        var vehicle = await createResponse.Content.ReadFromJsonAsync<VehicleResponse>();

        await UploadPhotoAsync(client, vehicle!.Id, "a.jpg", isPrimary: true);
        var secondPhotoResponse = await UploadPhotoAsync(client, vehicle.Id, "b.jpg", isPrimary: true);
        secondPhotoResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/api/v1/drivers/me/vehicles/{vehicle.Id}");
        var updated = await getResponse.Content.ReadFromJsonAsync<VehicleResponse>();
        updated!.Photos.Count.ShouldBe(2);
        updated.Photos.Count(p => p.IsPrimary).ShouldBe(1);
        updated.Photos.Single(p => p.IsPrimary).Url.ShouldContain("b.jpg");

        // Prove the returned URL is actually reachable — the static file middleware is wired
        // to the same physical path LocalFileStorage wrote to.
        var primaryPhotoUrl = updated.Photos.Single(p => p.IsPrimary).Url;
        var relativePath = new Uri(primaryPhotoUrl).PathAndQuery;
        var fetchResponse = await client.GetAsync(relativePath);
        fetchResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<HttpResponseMessage> UploadPhotoAsync(HttpClient client, Guid vehicleId, string fileName, bool isPrimary)
    {
        using var content = new MultipartFormDataContent();
        var bytes = "fake-image-bytes"u8.ToArray();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new("image/jpeg");
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(isPrimary.ToString()), "isPrimary");

        return await client.PostAsync($"/api/v1/drivers/me/vehicles/{vehicleId}/photos", content);
    }

    [Fact]
    public async Task SubmitLicenseVerification_SetsStatusToPending()
    {
        var client = await CreateClientWithDriverProfileAsync();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("fake-license-bytes"u8.ToArray());
        fileContent.Headers.ContentType = new("image/jpeg");
        content.Add(fileContent, "file", "license.jpg");

        var response = await client.PostAsync("/api/v1/drivers/me/driver-profile/verification-documents", content);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);

        var profileResponse = await client.GetAsync("/api/v1/drivers/me/driver-profile");
        var profile = await profileResponse.Content.ReadFromJsonAsync<DriverProfileResponse>();
        profile!.VerificationStatus.ShouldBe("Pending");
    }

    private sealed record DriverProfileResponse(Guid UserId, string LicenseNumber, bool IsVerifiedDriver, string VerificationStatus);

    private sealed record VehiclePhotoResponse(Guid Id, string Url, bool IsPrimary);

    private sealed record VehicleResponse(
        Guid Id, string Make, string Model, int Year, string Color, string RegistrationNumber,
        int SeatCapacity, bool IsVerified, List<VehiclePhotoResponse> Photos);
}
