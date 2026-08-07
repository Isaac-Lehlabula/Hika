namespace Hika.Application.Users.Dtos;

public sealed record VerifyEmailRequest
{
    public required Guid UserId { get; init; }

    public required string Token { get; init; }
}
