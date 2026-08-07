namespace Hika.Application.Users.Dtos;

public sealed record ResendVerificationEmailRequest
{
    public required string Email { get; init; }
}
