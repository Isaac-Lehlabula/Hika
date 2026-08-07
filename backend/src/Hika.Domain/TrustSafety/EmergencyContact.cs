using Hika.Domain.Common;

namespace Hika.Domain.TrustSafety;

/// <summary>Used for trip-sharing (a future "share my live trip" feature) — never exposed to
/// other platform users, see docs/domain-model.md §9.</summary>
public sealed class EmergencyContact : AuditableEntity
{
    public Guid UserId { get; private set; }

    public string Name { get; private set; }

    public string PhoneNumber { get; private set; }

    public string? Relationship { get; private set; }

    private EmergencyContact()
    {
        Name = string.Empty;
        PhoneNumber = string.Empty;
    }

    public static EmergencyContact Create(Guid userId, string name, string phoneNumber, string? relationship) => new()
    {
        UserId = userId,
        Name = name,
        PhoneNumber = phoneNumber,
        Relationship = relationship,
    };

    public void Update(string name, string phoneNumber, string? relationship)
    {
        Name = name;
        PhoneNumber = phoneNumber;
        Relationship = relationship;
    }
}
