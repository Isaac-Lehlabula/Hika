using Hika.Domain.Common;

namespace Hika.Domain.Users;

public sealed class EmailVerificationToken : AuditableEntity
{
    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    public bool IsUsable => UsedAtUtc is null && DateTimeOffset.UtcNow < ExpiresAtUtc;

    private EmailVerificationToken()
    {
        TokenHash = string.Empty;
    }

    public EmailVerificationToken(Guid userId, string tokenHash, DateTimeOffset expiresAtUtc)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void MarkUsed() => UsedAtUtc = DateTimeOffset.UtcNow;
}
