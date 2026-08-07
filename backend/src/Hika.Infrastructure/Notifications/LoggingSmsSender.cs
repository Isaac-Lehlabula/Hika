using Hika.Application.Users.Ports;
using Microsoft.Extensions.Logging;

namespace Hika.Infrastructure.Notifications;

/// <summary>
/// MVP stand-in — logs instead of sending. See docs/south-africa.md for real SA SMS
/// providers to evaluate (Clickatell, BulkSMS, Infobip). Swapping the implementation is a
/// one-line DI change; nothing above this interface needs to know.
/// </summary>
public sealed class LoggingSmsSender(ILogger<LoggingSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken)
    {
        logger.LogInformation("[DEV SMS to {PhoneNumber}]: {Message}", toPhoneNumber, message);
        return Task.CompletedTask;
    }
}
