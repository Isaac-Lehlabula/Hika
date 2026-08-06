namespace Hika.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAtUtc { get; protected set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; protected set; }

    public void MarkUpdated() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
