namespace Hika.Application.Users.Dtos;

public sealed record ForgotPasswordRequest
{
    public required string Email { get; init; }
}
