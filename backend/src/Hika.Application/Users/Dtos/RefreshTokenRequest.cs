namespace Hika.Application.Users.Dtos;

public sealed record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}
