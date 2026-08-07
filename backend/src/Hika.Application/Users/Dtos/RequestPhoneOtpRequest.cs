namespace Hika.Application.Users.Dtos;

public sealed record RequestPhoneOtpRequest
{
    /// <summary>E.164, e.g. +27821234567. Also updates the caller's profile phone number.</summary>
    public required string PhoneNumber { get; init; }
}
