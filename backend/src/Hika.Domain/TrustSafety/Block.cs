using Hika.Domain.Common;

namespace Hika.Domain.TrustSafety;

/// <summary>A blocked user is excluded from the blocker's search results and neither party can
/// book the other's trips (see docs/domain-model.md §9). One-directional by design — the
/// blocked user isn't notified and doesn't need to reciprocate for BookingService's
/// two-way interaction check to still stop them booking each other.</summary>
public sealed class Block : AuditableEntity
{
    public Guid BlockerUserId { get; private set; }

    public Guid BlockedUserId { get; private set; }

    private Block()
    {
    }

    public static Block Create(Guid blockerUserId, Guid blockedUserId) => new()
    {
        BlockerUserId = blockerUserId,
        BlockedUserId = blockedUserId,
    };
}
