namespace Hika.Application.Payments.Dtos;

public sealed record PaymentResponse
{
    public required Guid Id { get; init; }

    public required Guid BookingId { get; init; }

    public required decimal Amount { get; init; }

    public required decimal PlatformFee { get; init; }

    public required decimal DriverPayoutAmount { get; init; }

    public required string Provider { get; init; }

    public string? ProviderReference { get; init; }

    public required string Status { get; init; }

    /// <summary>Set only while Status is Pending on a redirect-based gateway (Ozow) — the
    /// client should open this externally and complete payment, then re-poll this endpoint.</summary>
    public string? RedirectUrl { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
