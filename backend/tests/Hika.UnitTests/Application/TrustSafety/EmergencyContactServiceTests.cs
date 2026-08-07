using Hika.Application.Common.Exceptions;
using Hika.Application.TrustSafety;
using Hika.Application.TrustSafety.Dtos;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.TrustSafety;

public class EmergencyContactServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly EmergencyContactService _sut;

    public EmergencyContactServiceTests()
    {
        _sut = new EmergencyContactService(_db);
    }

    private static EmergencyContactRequest ValidRequest() =>
        new() { Name = "Naledi Dlamini", PhoneNumber = "+27821234567", Relationship = "Sister" };

    [Fact]
    public async Task CreateAsync_ReturnsCreatedContact()
    {
        var contact = await _sut.CreateAsync(Guid.NewGuid(), ValidRequest(), CancellationToken.None);

        contact.Name.ShouldBe("Naledi Dlamini");
        contact.Relationship.ShouldBe("Sister");
    }

    [Fact]
    public async Task GetMyContactsAsync_OnlyReturnsCallersOwn()
    {
        var userId = Guid.NewGuid();
        await _sut.CreateAsync(userId, ValidRequest(), CancellationToken.None);
        await _sut.CreateAsync(Guid.NewGuid(), ValidRequest(), CancellationToken.None);

        var contacts = await _sut.GetMyContactsAsync(userId, CancellationToken.None);

        contacts.Count.ShouldBe(1);
    }

    [Fact]
    public async Task UpdateAsync_Owner_ChangesFields()
    {
        var userId = Guid.NewGuid();
        var created = await _sut.CreateAsync(userId, ValidRequest(), CancellationToken.None);

        var updated = await _sut.UpdateAsync(
            userId, created.Id, ValidRequest() with { Name = "Naledi M", Relationship = "Mother" }, CancellationToken.None);

        updated.Name.ShouldBe("Naledi M");
        updated.Relationship.ShouldBe("Mother");
    }

    [Fact]
    public async Task UpdateAsync_NotOwner_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        var created = await _sut.CreateAsync(userId, ValidRequest(), CancellationToken.None);

        await Should.ThrowAsync<NotFoundException>(
            () => _sut.UpdateAsync(Guid.NewGuid(), created.Id, ValidRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_Owner_RemovesContact()
    {
        var userId = Guid.NewGuid();
        var created = await _sut.CreateAsync(userId, ValidRequest(), CancellationToken.None);

        await _sut.DeleteAsync(userId, created.Id, CancellationToken.None);

        var remaining = await _sut.GetMyContactsAsync(userId, CancellationToken.None);
        remaining.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_ThrowsNotFoundAndDoesNotDelete()
    {
        var userId = Guid.NewGuid();
        var created = await _sut.CreateAsync(userId, ValidRequest(), CancellationToken.None);

        await Should.ThrowAsync<NotFoundException>(() => _sut.DeleteAsync(Guid.NewGuid(), created.Id, CancellationToken.None));

        var remaining = await _sut.GetMyContactsAsync(userId, CancellationToken.None);
        remaining.ShouldHaveSingleItem();
    }
}
