namespace Hika.Application.Users.Dtos;

public sealed record RegisterRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    /// <summary>E.164, e.g. +27821234567. Optional at registration — can be added/verified later from the profile.</summary>
    public string? PhoneNumber { get; init; }
}
