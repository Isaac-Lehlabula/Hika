namespace Hika.Application.Users.Dtos;

public sealed record VerifyPhoneRequest
{
    public required string Code { get; init; }
}
