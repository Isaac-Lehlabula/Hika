namespace Hika.Application.Users.Dtos;

public sealed record AuthTokenResponse
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset AccessTokenExpiresAtUtc { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTimeOffset RefreshTokenExpiresAtUtc { get; init; }
}
