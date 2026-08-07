using Hika.Domain.Common;

namespace Hika.Domain.Users;

/// <summary>Short-lived OTP sent via SMS. AttemptCount rate-limits guessing.</summary>
public sealed class PhoneVerificationCode : AuditableEntity
{
    private const int MaxAttempts = 5;

    public Guid UserId { get; private set; }

    public string PhoneNumber { get; private set; }

    public string CodeHash { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset? UsedAtUtc { get; private set; }

    public bool IsUsable => UsedAtUtc is null && AttemptCount < MaxAttempts && DateTimeOffset.UtcNow < ExpiresAtUtc;

    private PhoneVerificationCode()
    {
        PhoneNumber = string.Empty;
        CodeHash = string.Empty;
    }

    public PhoneVerificationCode(Guid userId, string phoneNumber, string codeHash, DateTimeOffset expiresAtUtc)
    {
        UserId = userId;
        PhoneNumber = phoneNumber;
        CodeHash = codeHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void RecordAttempt() => AttemptCount++;

    public void MarkUsed() => UsedAtUtc = DateTimeOffset.UtcNow;
}
