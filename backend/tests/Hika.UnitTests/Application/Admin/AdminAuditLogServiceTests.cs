using Hika.Application.Admin;
using Hika.Domain.Users;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.Admin;

public class AdminAuditLogServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly AdminAuditLogService _sut;

    public AdminAuditLogServiceTests()
    {
        _sut = new AdminAuditLogService(_db);
    }

    [Fact]
    public async Task GetLogsAsync_ResolvesAdminNameAndOrdersNewestFirst()
    {
        var admin = UserProfile.Create(Guid.NewGuid(), "Amahle", "Zulu");
        _db.UserProfiles.Add(admin);
        var logger = new AuditLogger(_db);
        logger.Record(admin.Id, "SuspendUser", "UserProfile", Guid.NewGuid(), "First offense");
        await _db.SaveChangesAsync(CancellationToken.None);
        logger.Record(admin.Id, "ApproveVerification", "Verification", Guid.NewGuid(), null);
        await _db.SaveChangesAsync(CancellationToken.None);

        var result = await _sut.GetLogsAsync(page: 1, pageSize: 20, CancellationToken.None);

        result.Items.Count.ShouldBe(2);
        result.Items[0].Action.ShouldBe("ApproveVerification");
        result.Items[0].AdminName.ShouldBe("Amahle Zulu");
    }
}
