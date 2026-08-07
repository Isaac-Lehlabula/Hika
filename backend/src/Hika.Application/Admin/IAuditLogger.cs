namespace Hika.Application.Admin;

public interface IAuditLogger
{
    /// <summary>Adds an AuditLog row to the current unit of work without saving — same
    /// pattern as INotificationDispatcher, so the calling service's own SaveChangesAsync
    /// persists the audit trail atomically alongside the action it describes.</summary>
    void Record(Guid adminUserId, string action, string entityType, Guid? entityId, string? details);
}
