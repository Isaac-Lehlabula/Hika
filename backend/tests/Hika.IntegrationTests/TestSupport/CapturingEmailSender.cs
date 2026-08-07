using System.Collections.Concurrent;
using Hika.Application.Users.Ports;

namespace Hika.IntegrationTests.TestSupport;

/// <summary>Replaces the real SMTP sender in integration tests so no Mailhog/SMTP server is required.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    public ConcurrentBag<(string To, string Subject, string Body)> SentEmails { get; } = [];

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        SentEmails.Add((toEmail, subject, htmlBody));
        return Task.CompletedTask;
    }

    /// <summary>Pulls the token/userId query params out of the first link found in an email body.</summary>
    public static string ExtractQueryParam(string body, string paramName)
    {
        var marker = $"{paramName}=";
        var start = body.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = body.IndexOfAny(['&', '"'], start);
        return Uri.UnescapeDataString(body[start..end]);
    }
}
