using System.Collections.Concurrent;
using Hika.Application.Users.Ports;

namespace Hika.IntegrationTests.TestSupport;

public sealed class CapturingSmsSender : ISmsSender
{
    public ConcurrentBag<(string To, string Message)> SentMessages { get; } = [];

    public Task SendAsync(string toPhoneNumber, string message, CancellationToken cancellationToken)
    {
        SentMessages.Add((toPhoneNumber, message));
        return Task.CompletedTask;
    }
}
