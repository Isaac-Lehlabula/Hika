using Hika.Application.Common.Exceptions;
using Hika.Application.RideAlerts;
using Hika.Application.RideAlerts.Dtos;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.RideAlerts;

public class RideAlertServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly RideAlertService _sut;

    public RideAlertServiceTests()
    {
        _sut = new RideAlertService(_db);
    }

    private static CreateRideAlertRequest ValidRequest() => new() { Origin = "Johannesburg", Destination = "Giyani" };

    [Fact]
    public async Task CreateAsync_ReturnsActiveAlert()
    {
        var alert = await _sut.CreateAsync(Guid.NewGuid(), ValidRequest(), CancellationToken.None);

        alert.Status.ShouldBe("Active");
        alert.OriginRawText.ShouldBe("Johannesburg");
        alert.DestinationRawText.ShouldBe("Giyani");
    }

    [Fact]
    public async Task GetMyAlertsAsync_OnlyReturnsCallersOwn()
    {
        var userId = Guid.NewGuid();
        await _sut.CreateAsync(userId, ValidRequest(), CancellationToken.None);
        await _sut.CreateAsync(Guid.NewGuid(), ValidRequest(), CancellationToken.None);

        var result = await _sut.GetMyAlertsAsync(userId, CancellationToken.None);

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteAsync_Owner_RemovesAlert()
    {
        var userId = Guid.NewGuid();
        var created = await _sut.CreateAsync(userId, ValidRequest(), CancellationToken.None);

        await _sut.DeleteAsync(userId, created.Id, CancellationToken.None);

        var remaining = await _sut.GetMyAlertsAsync(userId, CancellationToken.None);
        remaining.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ThrowsNotFoundAndDoesNotDelete()
    {
        var userId = Guid.NewGuid();
        var created = await _sut.CreateAsync(userId, ValidRequest(), CancellationToken.None);

        await Should.ThrowAsync<NotFoundException>(
            () => _sut.DeleteAsync(Guid.NewGuid(), created.Id, CancellationToken.None));

        var remaining = await _sut.GetMyAlertsAsync(userId, CancellationToken.None);
        remaining.ShouldHaveSingleItem();
    }
}
