namespace Hika.Application.Users.Dtos;

public sealed record ResetPasswordRequest
{
    public required Guid UserId { get; init; }

    public required string Token { get; init; }

    public required string NewPassword { get; init; }
}
