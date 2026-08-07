namespace Hika.Application.Notifications.Dtos;

public sealed record NotificationResponse
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public required string Message { get; init; }

    public Guid? RelatedEntityId { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset? ReadAtUtc { get; init; }
}
