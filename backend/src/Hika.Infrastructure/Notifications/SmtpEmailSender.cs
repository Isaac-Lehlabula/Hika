using Hika.Application.Users.Ports;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Hika.Infrastructure.Notifications;

/// <summary>SMTP delivery — points at Mailhog in local dev, a real relay/provider in production.</summary>
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var smtp = options.Value;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(smtp.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            var socketOptions = smtp.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrEmpty(smtp.Username) && !string.IsNullOrEmpty(smtp.Password))
            {
                await client.AuthenticateAsync(smtp.Username, smtp.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }

        logger.LogInformation("Sent email {Subject} to {ToEmail}", subject, toEmail);
    }
}
