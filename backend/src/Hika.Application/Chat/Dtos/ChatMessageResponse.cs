namespace Hika.Application.Chat.Dtos;

/// <summary>Mirrors Hika.Domain.Chat.ChatMessage, plus IsMine so the client doesn't have to
/// carry its own user id around just to decide which side of the thread a bubble renders on.</summary>
public sealed class ChatMessageResponse
{
    public required Guid Id { get; init; }

    public required Guid SenderUserId { get; init; }

    public required bool IsMine { get; init; }

    public required string Body { get; init; }

    public required DateTimeOffset SentAtUtc { get; init; }
}
