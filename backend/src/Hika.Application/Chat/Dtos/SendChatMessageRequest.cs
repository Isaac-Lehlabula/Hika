namespace Hika.Application.Chat.Dtos;

public sealed record SendChatMessageRequest
{
    public required string Message { get; init; }
}
