namespace Hika.Application.Admin.Dtos;

public sealed record SuspendUserRequest
{
    public required string Reason { get; init; }
}
