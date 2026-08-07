namespace Hika.Application.Users.Ports;

/// <summary>
/// MVP implementation sends via SMTP to Mailhog in dev (see Infrastructure). Swappable for a
/// transactional email provider later without any Application-layer change.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken);
}
