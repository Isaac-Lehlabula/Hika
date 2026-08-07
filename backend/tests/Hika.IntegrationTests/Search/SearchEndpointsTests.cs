using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Hika.Domain.Drivers;
using Hika.Infrastructure.Persistence;
using Hika.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Hika.IntegrationTests.Search;

public class SearchEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private static object VehicleBody(int seatCapacity = 4) => new
    {
        make = "Toyota",
        model = "Corolla",
        year = 2020,
        color = "White",
        registrationNumber = $"CA{Random.Shared.Next(100000, 999999)}",
        seatCapacity,
    };

    private async Task<(HttpClient Client, Guid DriverUserId, Guid VehicleId)> CreateClientWithVehicleAsync(
        int seatCapacity = 4, [CallerMemberName] string testName = "")
    {
        var (client, userId, _) = await factory.CreateAuthenticatedClientAsync(testName);
        (await client.PostAsJsonAsync("/api/v1/drivers/me/driver-profile", new
        {
            licenseNumber = "1234567890123",
            licenseExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(2)),
        })).EnsureSuccessStatusCode();

        var vehicleResponse = await client.PostAsJsonAsync("/api/v1/drivers/me/vehicles", VehicleBody(seatCapacity));
        vehicleResponse.EnsureSuccessStatusCode();
        var vehicle = await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>();

        return (client, userId, vehicle!.Id);
    }

    private static readonly object[] JohannesburgToGiyaniStops =
    [
        new { rawName = "Johannesburg", province = "Gauteng" },
        new { rawName = "Polokwane", province = "Limpopo" },
        new { rawName = "Giyani", province = "Limpopo" },
    ];

    private async Task<Guid> PostTripAsync(
        HttpClient client, Guid vehicleId, DateTimeOffset departureAtUtc, int seats, decimal price, object[]? stops = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/trips", new
        {
            vehicleId,
            departureAtUtc,
            totalSeatsOffered = seats,
            pricePerSeat = price,
            stops = stops ?? JohannesburgToGiyaniStops,
        });
        response.EnsureSuccessStatusCode();
        var trip = await response.Content.ReadFromJsonAsync<TripResponse>();
        return trip!.Id;
    }

    [Fact]
    public async Task SearchTrips_MatchesRequestedSubLegNotFullRoute()
    {
        var (client, _, vehicleId) = await CreateClientWithVehicleAsync();
        var tripId = await PostTripAsync(client, vehicleId, DateTimeOffset.UtcNow.AddDays(2), seats: 4, price: 300m);

        var anonymous = factory.CreateClient();
        var response = await anonymous.GetAsync("/api/v1/search/trips?from=johannesburg&to=polokwane");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedSearchResponse>();
        result!.Items.ShouldContain(t => t.Id == tripId);
        var match = result.Items.Single(t => t.Id == tripId);
        match.BoardingStopName.ShouldBe("Johannesburg");
        match.AlightingStopName.ShouldBe("Polokwane");
        match.SeatsAvailable.ShouldBe(4);
    }

    [Fact]
    public async Task SearchTrips_UnmatchedRoute_ReturnsEmpty()
    {
        var (client, _, vehicleId) = await CreateClientWithVehicleAsync();
        await PostTripAsync(client, vehicleId, DateTimeOffset.UtcNow.AddDays(2), seats: 4, price: 300m);

        var response = await factory.CreateClient().GetAsync("/api/v1/search/trips?from=capetown&to=durban");

        var result = await response.Content.ReadFromJsonAsync<PagedSearchResponse>();
        result!.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task SearchTrips_PassengersExceedingSeatsAvailable_ExcludesTrip()
    {
        var (client, _, vehicleId) = await CreateClientWithVehicleAsync(seatCapacity: 2);
        var tripId = await PostTripAsync(client, vehicleId, DateTimeOffset.UtcNow.AddDays(2), seats: 2, price: 300m);

        var response = await factory.CreateClient().GetAsync("/api/v1/search/trips?from=johannesburg&to=giyani&passengers=3");

        var result = await response.Content.ReadFromJsonAsync<PagedSearchResponse>();
        result!.Items.ShouldNotContain(t => t.Id == tripId);
    }

    [Fact]
    public async Task SearchTrips_MaxPriceFilter_ExcludesExpensiveTrips()
    {
        var (client, _, vehicleId) = await CreateClientWithVehicleAsync();
        var tripId = await PostTripAsync(client, vehicleId, DateTimeOffset.UtcNow.AddDays(2), seats: 4, price: 500m);

        var tooLow = await factory.CreateClient().GetAsync("/api/v1/search/trips?from=johannesburg&to=giyani&maxPrice=300");
        var tooLowResult = await tooLow.Content.ReadFromJsonAsync<PagedSearchResponse>();
        tooLowResult!.Items.ShouldNotContain(t => t.Id == tripId);

        var highEnough = await factory.CreateClient().GetAsync("/api/v1/search/trips?from=johannesburg&to=giyani&maxPrice=500");
        var highEnoughResult = await highEnough.Content.ReadFromJsonAsync<PagedSearchResponse>();
        highEnoughResult!.Items.ShouldContain(t => t.Id == tripId);
    }

    [Fact]
    public async Task SearchTrips_DateFilter_OnlyMatchesThatCalendarDay()
    {
        var (client, _, vehicleId) = await CreateClientWithVehicleAsync();
        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var onTarget = await PostTripAsync(
            client, vehicleId, new DateTimeOffset(targetDate.ToDateTime(new TimeOnly(8, 0)), TimeSpan.FromHours(2)).ToUniversalTime(), seats: 4, price: 300m);
        var otherDay = await PostTripAsync(client, vehicleId, DateTimeOffset.UtcNow.AddDays(20), seats: 4, price: 300m);

        var response = await factory.CreateClient().GetAsync($"/api/v1/search/trips?from=johannesburg&to=giyani&date={targetDate:yyyy-MM-dd}");

        var result = await response.Content.ReadFromJsonAsync<PagedSearchResponse>();
        result!.Items.ShouldContain(t => t.Id == onTarget);
        result.Items.ShouldNotContain(t => t.Id == otherDay);
    }

    [Fact]
    public async Task SearchTrips_VerifiedOnlyFilter_ExcludesUnverifiedDrivers()
    {
        var (client, driverUserId, vehicleId) = await CreateClientWithVehicleAsync();
        var tripId = await PostTripAsync(client, vehicleId, DateTimeOffset.UtcNow.AddDays(2), seats: 4, price: 300m);

        var unverifiedResponse = await factory.CreateClient().GetAsync("/api/v1/search/trips?from=johannesburg&to=giyani&verifiedOnly=true");
        var unverifiedResult = await unverifiedResponse.Content.ReadFromJsonAsync<PagedSearchResponse>();
        unverifiedResult!.Items.ShouldNotContain(t => t.Id == tripId);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var driverProfile = await db.DriverProfiles.SingleAsync(d => d.Id == driverUserId);
            driverProfile.MarkVerified();
            await db.SaveChangesAsync();
        }

        var verifiedResponse = await factory.CreateClient().GetAsync("/api/v1/search/trips?from=johannesburg&to=giyani&verifiedOnly=true");
        var verifiedResult = await verifiedResponse.Content.ReadFromJsonAsync<PagedSearchResponse>();
        verifiedResult!.Items.ShouldContain(t => t.Id == tripId);
    }

    [Fact]
    public async Task SearchTrips_SortByPrice_OrdersAscending()
    {
        var (client, _, vehicleId) = await CreateClientWithVehicleAsync();
        var expensive = await PostTripAsync(client, vehicleId, DateTimeOffset.UtcNow.AddDays(2), seats: 4, price: 500m);
        var cheap = await PostTripAsync(client, vehicleId, DateTimeOffset.UtcNow.AddDays(3), seats: 4, price: 250m);

        var response = await factory.CreateClient().GetAsync("/api/v1/search/trips?from=johannesburg&to=giyani&sort=Price");

        var result = await response.Content.ReadFromJsonAsync<PagedSearchResponse>();
        var ids = result!.Items.Select(t => t.Id).ToList();
        ids.IndexOf(cheap).ShouldBeLessThan(ids.IndexOf(expensive));
    }

    [Fact]
    public async Task SearchLocations_MatchesSeededCitiesCaseInsensitively()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/search/locations?query=johann");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var locations = await response.Content.ReadFromJsonAsync<List<LocationResponse>>();
        locations!.ShouldContain(l => l.Name == "Johannesburg");
    }

    [Fact]
    public async Task GetPopularRoutes_AggregatesRealTripsNotHardcoded()
    {
        var (client, _, vehicleId) = await CreateClientWithVehicleAsync();
        var month = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(2));
        var departure = new DateTimeOffset(month.ToDateTime(new TimeOnly(8, 0)), TimeSpan.FromHours(2)).ToUniversalTime();
        await PostTripAsync(client, vehicleId, departure, seats: 4, price: 300m);
        await PostTripAsync(client, vehicleId, departure.AddHours(1), seats: 4, price: 300m);

        var response = await factory.CreateClient().GetAsync($"/api/v1/search/popular-routes?month={month:yyyy-MM-dd}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var routes = await response.Content.ReadFromJsonAsync<List<PopularRouteResponse>>();
        var route = routes!.SingleOrDefault(r => r.OriginName == "Johannesburg" && r.DestinationName == "Giyani");
        route.ShouldNotBeNull();
        route.TripCount.ShouldBe(2);
    }

    private sealed record VehicleResponse(Guid Id);

    private sealed record TripResponse(Guid Id);

    private sealed record SearchTripItem(Guid Id, string BoardingStopName, string AlightingStopName, int SeatsAvailable);

    private sealed record PagedSearchResponse(List<SearchTripItem> Items, int Page, int PageSize, int TotalCount);

    private sealed record LocationResponse(Guid Id, string Name, string Province, string Type);

    private sealed record PopularRouteResponse(string OriginName, string DestinationName, int TripCount);
}
