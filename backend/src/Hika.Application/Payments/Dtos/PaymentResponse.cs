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

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
