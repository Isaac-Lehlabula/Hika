namespace Hika.Application.Users.Dtos;

public sealed record UpdateProfileRequest
{
    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public string? PhotoUrl { get; init; }
}
