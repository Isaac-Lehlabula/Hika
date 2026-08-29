namespace Hika.Application.Notifications.Ports;

/// <summary>
/// MVP implementation logs instead of sending real pushes — no Firebase project exists in this
/// environment (see docs/south-africa.md and NotificationDispatcher's remarks). Swappable
/// without any Application-layer change, same pattern as IEmailSender/ISmsSender.
/// </summary>
public interface IPushSender
{
    Task SendAsync(
        IReadOnlyCollection<string> deviceTokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken cancellationToken);
}
