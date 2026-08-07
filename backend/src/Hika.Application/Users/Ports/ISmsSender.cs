namespace Hika.Application.Users.Ports;

/// <summary>
/// MVP implementation logs instead of sending (see docs/south-africa.md for SMS providers to
/// evaluate — Clickatell, BulkSMS, Infobip). Swappable without any Application-layer change.
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken);
}
