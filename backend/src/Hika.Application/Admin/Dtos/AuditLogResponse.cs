namespace Hika.Application.Admin.Dtos;

public sealed record AuditLogResponse
{
    public required Guid Id { get; init; }

    public required Guid AdminUserId { get; init; }

    public required string AdminName { get; init; }

    public required string Action { get; init; }

    public required string EntityType { get; init; }

    public Guid? EntityId { get; init; }

    public string? Details { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
