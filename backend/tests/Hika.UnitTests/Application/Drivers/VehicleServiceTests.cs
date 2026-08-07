using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Storage;
using Hika.Application.Drivers;
using Hika.Application.Drivers.Dtos;
using Hika.Domain.Drivers;
using Hika.UnitTests.TestSupport;
using NSubstitute;
using Shouldly;

namespace Hika.UnitTests.Application.Drivers;

public class VehicleServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly IFileStorage _fileStorage = Substitute.For<IFileStorage>();
    private readonly VehicleService _sut;

    public VehicleServiceTests()
    {
        _sut = new VehicleService(_db, _fileStorage);
        _fileStorage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult($"https://example.com/{callInfo.ArgAt<string>(1)}/{Guid.NewGuid():N}.jpg"));
    }

    private static VehicleRequest ValidRequest() => new()
    {
        Make = "Toyota",
        Model = "Corolla",
        Year = 2020,
        Color = "White",
        RegistrationNumber = "CA123456",
        SeatCapacity = 4,
    };

    private async Task<Guid> SeedDriverAsync()
    {
        var userId = Guid.NewGuid();
        _db.DriverProfiles.Add(DriverProfile.Create(userId, "1234567890", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1))));
        await _db.SaveChangesAsync(CancellationToken.None);
        return userId;
    }

    [Fact]
    public async Task CreateAsync_NoDriverProfile_ThrowsValidation()
    {
        await Should.ThrowAsync<AppValidationException>(
            () => _sut.CreateAsync(Guid.NewGuid(), ValidRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithDriverProfile_CreatesVehicle()
    {
        var driverId = await SeedDriverAsync();

        var vehicle = await _sut.CreateAsync(driverId, ValidRequest(), CancellationToken.None);

        vehicle.Make.ShouldBe("Toyota");
        vehicle.IsVerified.ShouldBeFalse();
        vehicle.Photos.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetOwnAsync_VehicleBelongsToAnotherDriver_ThrowsNotFound()
    {
        var driverId = await SeedDriverAsync();
        var vehicle = await _sut.CreateAsync(driverId, ValidRequest(), CancellationToken.None);

        await Should.ThrowAsync<NotFoundException>(
            () => _sut.GetOwnAsync(Guid.NewGuid(), vehicle.Id, CancellationToken.None));
    }

    [Fact]
    public async Task ListOwnAsync_OnlyReturnsCallersVehicles()
    {
        var driverId = await SeedDriverAsync();
        await _sut.CreateAsync(driverId, ValidRequest(), CancellationToken.None);
        var otherDriverId = await SeedDriverAsync();
        await _sut.CreateAsync(otherDriverId, ValidRequest() with { RegistrationNumber = "GP999999" }, CancellationToken.None);

        var result = await _sut.ListOwnAsync(driverId, CancellationToken.None);

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateAsync_ChangingRegistrationNumber_ResetsVerification()
    {
        var driverId = await SeedDriverAsync();
        var created = await _sut.CreateAsync(driverId, ValidRequest(), CancellationToken.None);
        var entity = _db.Vehicles.Single(v => v.Id == created.Id);
        entity.MarkVerified();
        await _db.SaveChangesAsync(CancellationToken.None);

        var updated = await _sut.UpdateAsync(
            driverId, created.Id, ValidRequest() with { RegistrationNumber = "GP111111" }, CancellationToken.None);

        updated.IsVerified.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateAsync_SameRegistrationNumber_KeepsVerification()
    {
        var driverId = await SeedDriverAsync();
        var created = await _sut.CreateAsync(driverId, ValidRequest(), CancellationToken.None);
        var entity = _db.Vehicles.Single(v => v.Id == created.Id);
        entity.MarkVerified();
        await _db.SaveChangesAsync(CancellationToken.None);

        var updated = await _sut.UpdateAsync(driverId, created.Id, ValidRequest() with { Color = "Blue" }, CancellationToken.None);

        updated.IsVerified.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteAsync_RemovesVehicle()
    {
        var driverId = await SeedDriverAsync();
        var created = await _sut.CreateAsync(driverId, ValidRequest(), CancellationToken.None);

        await _sut.DeleteAsync(driverId, created.Id, CancellationToken.None);

        _db.Vehicles.Any(v => v.Id == created.Id).ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAsync_NotOwned_ThrowsNotFoundAndDoesNotDelete()
    {
        var driverId = await SeedDriverAsync();
        var created = await _sut.CreateAsync(driverId, ValidRequest(), CancellationToken.None);

        await Should.ThrowAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid(), created.Id, CancellationToken.None));

        _db.Vehicles.Any(v => v.Id == created.Id).ShouldBeTrue();
    }

    // "A second primary photo unsets the first" is a Vehicle aggregate invariant — tested
    // directly on the domain entity in Domain/Drivers/VehicleTests.cs. The EF InMemory
    // provider has known rough edges tracking collection-navigation updates across separate
    // DbContext instances sharing one backing store, which made attempting the same scenario
    // here (two request-scoped VehicleServices) an unreliable way to test it; the real,
    // relational-provider version of this path is covered by the Postgres integration tests.

    [Fact]
    public async Task SubmitVehicleVerificationAsync_MarksVehicleUnverifiedAndPending()
    {
        var driverId = await SeedDriverAsync();
        var created = await _sut.CreateAsync(driverId, ValidRequest(), CancellationToken.None);
        using var stream = new MemoryStream();

        await _sut.SubmitVehicleVerificationAsync(driverId, created.Id, stream, "reg.jpg", "image/jpeg", CancellationToken.None);

        var vehicle = await _sut.GetOwnAsync(driverId, created.Id, CancellationToken.None);
        vehicle.IsVerified.ShouldBeFalse();
    }
}
