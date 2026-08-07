using Hika.Domain.Admin;
using Shouldly;

namespace Hika.UnitTests.Domain.Admin;

public class AuditLogTests
{
    [Fact]
    public void Record_CapturesAllFields()
    {
        var adminUserId = Guid.NewGuid();
        var entityId = Guid.NewGuid();

        var log = AuditLog.Record(adminUserId, "SuspendUser", "UserProfile", entityId, "Repeated no-shows");

        log.AdminUserId.ShouldBe(adminUserId);
        log.Action.ShouldBe("SuspendUser");
        log.EntityType.ShouldBe("UserProfile");
        log.EntityId.ShouldBe(entityId);
        log.Details.ShouldBe("Repeated no-shows");
    }

    [Fact]
    public void Record_WithoutEntityIdOrDetails_AllowsNulls()
    {
        var log = AuditLog.Record(Guid.NewGuid(), "UpdatePlatformFee", "PlatformFeeSettings", null, null);

        log.EntityId.ShouldBeNull();
        log.Details.ShouldBeNull();
    }
}
