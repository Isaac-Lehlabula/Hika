using Hika.Application.Notifications.Ports;
using Microsoft.Extensions.Logging;

namespace Hika.Infrastructure.Notifications;

/// <summary>
/// MVP stand-in — logs instead of sending. No Firebase project exists in this environment; see
/// FirebasePushSender's remarks and docs/south-africa.md. Swapping the implementation is a
/// one-line DI change (see DependencyInjection.AddInfrastructure), same pattern as
/// LoggingSmsSender.
/// </summary>
public sealed class LoggingPushSender(ILogger<LoggingPushSender> logger) : IPushSender
{
    public Task SendAsync(
        IReadOnlyCollection<string> deviceTokens, string title, string body, IReadOnlyDictionary<string, string>? data, CancellationToken cancellationToken)
    {
        logger.LogInformation("[DEV PUSH to {TokenCount} device(s)] {Title}: {Body}", deviceTokens.Count, title, body);
        return Task.CompletedTask;
    }
}
