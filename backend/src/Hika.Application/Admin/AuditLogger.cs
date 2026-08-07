using Hika.Application.Common.Persistence;
using Hika.Domain.Admin;

namespace Hika.Application.Admin;

public sealed class AuditLogger(IAppDbContext db) : IAuditLogger
{
    public void Record(Guid adminUserId, string action, string entityType, Guid? entityId, string? details) =>
        db.AuditLogs.Add(AuditLog.Record(adminUserId, action, entityType, entityId, details));
}
