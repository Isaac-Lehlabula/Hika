using Hika.Application.Admin;
using Hika.Domain.Admin;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.Admin;

public class AdminPlatformFeeServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly AdminPlatformFeeService _sut;

    public AdminPlatformFeeServiceTests()
    {
        _sut = new AdminPlatformFeeService(_db, new AuditLogger(_db));
    }

    [Fact]
    public async Task GetAsync_NoSettingsYet_LazilyCreatesDefault()
    {
        var result = await _sut.GetAsync(CancellationToken.None);

        result.Rate.ShouldBe(PlatformFeeSettings.DefaultRate);
    }

    [Fact]
    public async Task UpdateAsync_ChangesRate_AndPersists()
    {
        var adminUserId = Guid.NewGuid();

        var result = await _sut.UpdateAsync(adminUserId, 0.2m, CancellationToken.None);

        result.Rate.ShouldBe(0.2m);
        result.UpdatedByAdminUserId.ShouldBe(adminUserId);

        var fetched = await _sut.GetAsync(CancellationToken.None);
        fetched.Rate.ShouldBe(0.2m);
    }
}
