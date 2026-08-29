using Hika.Application.Common.Exceptions;
using Hika.Application.Notifications;
using Hika.Application.Notifications.Ports;
using Hika.Application.Trips;
using Hika.Application.Trips.Dtos;
using Hika.Domain.Common;
using Hika.Domain.Drivers;
using Hika.UnitTests.TestSupport;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Hika.UnitTests.Application.Trips;

public class TripServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly TripService _sut;

    public TripServiceTests()
    {
        _sut = new TripService(
            _db, new NotificationDispatcher(_db, Substitute.For<IPushSender>(), Substitute.For<ILogger<NotificationDispatcher>>()));
    }

    private async Task<(Guid DriverId, Guid VehicleId)> SeedDriverWithVehicleAsync(int seatCapacity = 4)
    {
        var userId = Guid.NewGuid();
        _db.DriverProfiles.Add(DriverProfile.Create(userId, "1234567890", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))));
        var vehicle = Vehicle.Create(userId, "Toyota", "Corolla", 2020, "White", $"CA{Random.Shared.Next(100000, 999999)}", seatCapacity);
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync(CancellationToken.None);
        return (userId, vehicle.Id);
    }

    private static CreateTripRequest ValidRequest(Guid vehicleId, int totalSeatsOffered = 4) => new()
    {
        VehicleId = vehicleId,
        DepartureAtUtc = DateTimeOffset.UtcNow.AddDays(1),
        TotalSeatsOffered = totalSeatsOffered,
        PricePerSeat = 300m,
        LuggageAllowance = "One bag per passenger",
        Notes = "No smoking",
        Stops =
        [
            new() { RawName = "Johannesburg", Province = Province.Gauteng },
            new() { RawName = "Polokwane", Province = Province.Limpopo },
            new() { RawName = "Giyani", Province = Province.Limpopo },
        ],
    };

    [Fact]
    public async Task CreateAsync_NoDriverProfile_ThrowsValidation()
    {
        await Should.ThrowAsync<AppValidationException>(
            () => _sut.CreateAsync(Guid.NewGuid(), ValidRequest(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_VehicleBelongsToAnotherDriver_ThrowsValidation()
    {
        var (_, vehicleId) = await SeedDriverWithVehicleAsync();
        var otherDriverId = Guid.NewGuid();
        _db.DriverProfiles.Add(DriverProfile.Create(otherDriverId, "0987654321", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))));
        await _db.SaveChangesAsync(CancellationToken.None);

        await Should.ThrowAsync<AppValidationException>(
            () => _sut.CreateAsync(otherDriverId, ValidRequest(vehicleId), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_SeatsExceedVehicleCapacity_ThrowsValidation()
    {
        var (driverId, vehicleId) = await SeedDriverWithVehicleAsync(seatCapacity: 4);

        await Should.ThrowAsync<AppValidationException>(
            () => _sut.CreateAsync(driverId, ValidRequest(vehicleId, totalSeatsOffered: 5), CancellationToken.None));
    }

    // TripService's read paths (GetAsync, GetMyTripsAsync, and CancelAsync's own lookup) all
    // materialize a Trip — which carries a non-nullable Money ComplexProperty — via a LINQ
    // query. The EF Core 10 InMemory provider's query shaper cannot currently build a shaped
    // query for an entity with a required ComplexProperty (KeyNotFoundException deep inside
    // InMemoryShapedQueryCompilingExpressionVisitor, unrelated to how Money itself is
    // configured — Trip.Create's stop/segment generation is covered without EF at all in
    // Domain/Trips/TripTests.cs). This is the same class of InMemory-provider rough edge noted
    // in VehicleServiceTests; the full create-then-read-then-cancel round trip against the real
    // relational provider is covered by Hika.IntegrationTests/Trips/TripsEndpointsTests.cs.
    // Here we keep to CreateAsync's pre-flight validation guards, which only query
    // DriverProfiles/Vehicles and never materialize a Trip.
}
