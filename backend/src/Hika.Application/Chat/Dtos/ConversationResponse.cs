namespace Hika.Application.Chat.Dtos;

public sealed class ConversationResponse
{
    public required Guid Id { get; init; }

    public required Guid BookingId { get; init; }

    public required bool IsOpen { get; init; }

    public required IReadOnlyList<ChatMessageResponse> Messages { get; init; }
}
