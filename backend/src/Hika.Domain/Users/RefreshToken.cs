using Hika.Domain.Common;

namespace Hika.Domain.Users;

/// <summary>
/// Only the hash of the token value is ever stored — a DB leak doesn't hand out usable
/// tokens. Rotated on every refresh; reuse of an already-rotated token is theft evidence
/// and revokes the whole chain (see AuthService.RefreshAsync).
/// </summary>
public sealed class RefreshToken : AuditableEntity
{
    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? DeviceInfo { get; private set; }

    public bool IsActive => RevokedAtUtc is null && DateTimeOffset.UtcNow < ExpiresAtUtc;

    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAtUtc, string? createdByIp, string? deviceInfo)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
        DeviceInfo = deviceInfo;
    }

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAtUtc = DateTimeOffset.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
