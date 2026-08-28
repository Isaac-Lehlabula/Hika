using Hika.Domain.Common;

namespace Hika.Domain.Chat;

public sealed class ChatMessage : AuditableEntity
{
    public const int MaxBodyLength = 2000;

    public Guid ConversationId { get; private set; }

    public Guid SenderUserId { get; private set; }

    public string Body { get; private set; } = null!;

    public DateTimeOffset SentAtUtc { get; private set; }

    private ChatMessage()
    {
    }

    public static ChatMessage Send(Guid conversationId, Guid senderUserId, string body)
    {
        var trimmed = body?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Message can't be empty.", nameof(body));
        }

        if (trimmed.Length > MaxBodyLength)
        {
            throw new ArgumentException($"Message can't be longer than {MaxBodyLength} characters.", nameof(body));
        }

        return new ChatMessage
        {
            ConversationId = conversationId,
            SenderUserId = senderUserId,
            Body = trimmed,
            SentAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
