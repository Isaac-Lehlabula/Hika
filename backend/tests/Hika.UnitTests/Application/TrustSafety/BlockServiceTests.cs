using Hika.Application.Common.Exceptions;
using Hika.Application.TrustSafety;
using Hika.Domain.Users;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.TrustSafety;

public class BlockServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly BlockService _sut;

    public BlockServiceTests()
    {
        _sut = new BlockService(_db);
    }

    private async Task<Guid> SeedUserAsync(string firstName = "Thabo")
    {
        var userId = Guid.NewGuid();
        _db.UserProfiles.Add(UserProfile.Create(userId, firstName, "Mokoena"));
        await _db.SaveChangesAsync(CancellationToken.None);
        return userId;
    }

    [Fact]
    public async Task BlockAsync_Self_ThrowsValidation()
    {
        var userId = await SeedUserAsync();

        await Should.ThrowAsync<AppValidationException>(() => _sut.BlockAsync(userId, userId, CancellationToken.None));
    }

    [Fact]
    public async Task BlockAsync_ThenGetMyBlocks_ReturnsTheBlockedUser()
    {
        var blockerId = await SeedUserAsync("Thabo");
        var blockedId = await SeedUserAsync("Naledi");

        await _sut.BlockAsync(blockerId, blockedId, CancellationToken.None);

        var blocks = await _sut.GetMyBlocksAsync(blockerId, CancellationToken.None);
        blocks.ShouldHaveSingleItem();
        blocks[0].UserId.ShouldBe(blockedId);
        blocks[0].FirstName.ShouldBe("Naledi");
    }

    [Fact]
    public async Task BlockAsync_CalledTwice_IsIdempotent()
    {
        var blockerId = await SeedUserAsync("Thabo");
        var blockedId = await SeedUserAsync("Naledi");

        await _sut.BlockAsync(blockerId, blockedId, CancellationToken.None);
        await _sut.BlockAsync(blockerId, blockedId, CancellationToken.None);

        var blocks = await _sut.GetMyBlocksAsync(blockerId, CancellationToken.None);
        blocks.Count.ShouldBe(1);
    }

    [Fact]
    public async Task UnblockAsync_RemovesTheBlock()
    {
        var blockerId = await SeedUserAsync("Thabo");
        var blockedId = await SeedUserAsync("Naledi");
        await _sut.BlockAsync(blockerId, blockedId, CancellationToken.None);

        await _sut.UnblockAsync(blockerId, blockedId, CancellationToken.None);

        var blocks = await _sut.GetMyBlocksAsync(blockerId, CancellationToken.None);
        blocks.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnblockAsync_NotCurrentlyBlocked_DoesNotThrow()
    {
        var blockerId = await SeedUserAsync("Thabo");
        var blockedId = await SeedUserAsync("Naledi");

        await Should.NotThrowAsync(() => _sut.UnblockAsync(blockerId, blockedId, CancellationToken.None));
    }

    [Fact]
    public async Task GetMyBlocksAsync_OnlyReturnsCallersOwn()
    {
        var blockerId = await SeedUserAsync("Thabo");
        var otherBlockerId = await SeedUserAsync("Sipho");
        var blockedId = await SeedUserAsync("Naledi");
        await _sut.BlockAsync(otherBlockerId, blockedId, CancellationToken.None);

        var blocks = await _sut.GetMyBlocksAsync(blockerId, CancellationToken.None);

        blocks.ShouldBeEmpty();
    }
}
