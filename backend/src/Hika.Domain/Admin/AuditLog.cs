using Hika.Domain.Common;

namespace Hika.Domain.Admin;

/// <summary>
/// One row per sensitive admin action (suspend a user, approve/reject a verification, remove
/// a trip, issue a refund, resolve/dismiss a report, delete a review, change the platform fee)
/// — append-only, never updated, so AuditableEntity's UpdatedAtUtc doesn't apply here.
/// EntityId/EntityType identify what was acted on; Details is a short human-readable summary,
/// not a structured diff — this is a trail for staff to read, not a replay log.
/// </summary>
public sealed class AuditLog : Entity
{
    public Guid AdminUserId { get; private set; }

    public string Action { get; private set; }

    public string EntityType { get; private set; }

    public Guid? EntityId { get; private set; }

    public string? Details { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private AuditLog()
    {
        Action = string.Empty;
        EntityType = string.Empty;
    }

    public static AuditLog Record(Guid adminUserId, string action, string entityType, Guid? entityId, string? details) => new()
    {
        AdminUserId = adminUserId,
        Action = action,
        EntityType = entityType,
        EntityId = entityId,
        Details = details,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };
}
