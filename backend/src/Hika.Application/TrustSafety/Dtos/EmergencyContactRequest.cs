namespace Hika.Application.TrustSafety.Dtos;

public sealed record EmergencyContactRequest
{
    public required string Name { get; init; }

    public required string PhoneNumber { get; init; }

    public string? Relationship { get; init; }
}
