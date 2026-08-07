using Hika.Application.Admin;
using Hika.Application.Users.Ports;
using Hika.Domain.Users;
using Hika.UnitTests.TestSupport;
using NSubstitute;
using Shouldly;

namespace Hika.UnitTests.Application.Admin;

public class AdminUserServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly IUserAccountService _userAccounts = Substitute.For<IUserAccountService>();
    private readonly AdminUserService _sut;

    public AdminUserServiceTests()
    {
        _sut = new AdminUserService(_db, _userAccounts, new AuditLogger(_db));
    }

    private UserProfile SeedProfile(string firstName, string lastName, string email)
    {
        var profile = UserProfile.Create(Guid.NewGuid(), firstName, lastName);
        _db.UserProfiles.Add(profile);
        _db.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();

        _userAccounts.FindByIdAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(new UserAccountSummary(profile.Id, email, true));

        return profile;
    }

    [Fact]
    public async Task GetUsersAsync_WithSearch_MatchesFirstOrLastName()
    {
        SeedProfile("Thabo", "Nkosi", "thabo@example.com");
        var maria = SeedProfile("Maria", "Van Wyk", "maria@example.com");
        _userAccounts.FindByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.ArgAt<IReadOnlyCollection<Guid>>(0)
                .Select(id => new UserAccountSummary(id, id == maria.Id ? "maria@example.com" : "thabo@example.com", true))
                .ToList());

        var result = await _sut.GetUsersAsync("van wyk", page: 1, pageSize: 20, CancellationToken.None);

        result.Items.ShouldHaveSingleItem();
        result.Items[0].UserId.ShouldBe(maria.Id);
    }

    [Fact]
    public async Task SuspendAsync_SetsIsSuspendedAndReason()
    {
        var profile = SeedProfile("Thabo", "Nkosi", "thabo@example.com");

        var result = await _sut.SuspendAsync(Guid.NewGuid(), profile.Id, "Repeated no-shows", CancellationToken.None);

        result.IsSuspended.ShouldBeTrue();
        result.SuspensionReason.ShouldBe("Repeated no-shows");
    }

    [Fact]
    public async Task UnsuspendAsync_ClearsSuspendedState()
    {
        var profile = SeedProfile("Thabo", "Nkosi", "thabo@example.com");
        await _sut.SuspendAsync(Guid.NewGuid(), profile.Id, "Repeated no-shows", CancellationToken.None);

        var result = await _sut.UnsuspendAsync(Guid.NewGuid(), profile.Id, CancellationToken.None);

        result.IsSuspended.ShouldBeFalse();
        result.SuspensionReason.ShouldBeNull();
    }
}
