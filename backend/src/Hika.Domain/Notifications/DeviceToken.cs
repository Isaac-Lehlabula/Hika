using Hika.Domain.Common;

namespace Hika.Domain.Notifications;

public enum DevicePlatform
{
    Android,
    Ios,
    Web,
}

/// <summary>
/// An FCM registration token for one installed app instance. A token belongs to whichever
/// account was last logged in on that device — see DeviceTokenService.RegisterAsync's remarks
/// on why re-registering an existing token reassigns it rather than erroring, which matters for
/// shared/reused devices during testing (and for anyone who logs into a second account on the
/// same phone).
/// </summary>
public sealed class DeviceToken : AuditableEntity
{
    public Guid UserId { get; private set; }

    public string Token { get; private set; }

    public DevicePlatform Platform { get; private set; }

    private DeviceToken()
    {
        Token = string.Empty;
    }

    public static DeviceToken Register(Guid userId, string token, DevicePlatform platform) => new()
    {
        UserId = userId,
        Token = token,
        Platform = platform,
    };

    /// <summary>Called when an existing token row is found for a token being re-registered by a
    /// different (or the same) user — see the class remarks.</summary>
    public void ReassignTo(Guid userId) => UserId = userId;
}
