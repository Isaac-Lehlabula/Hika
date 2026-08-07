namespace Hika.Application.Users.Ports;

public interface IJwtTokenGenerator
{
    GeneratedAccessToken GenerateAccessToken(Guid userId, string email);
}

public sealed record GeneratedAccessToken(string Token, DateTimeOffset ExpiresAtUtc);
