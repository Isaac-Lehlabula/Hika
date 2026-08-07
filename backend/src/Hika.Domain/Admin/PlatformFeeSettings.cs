using Hika.Domain.Common;

namespace Hika.Domain.Admin;

/// <summary>
/// Single-row settings table (fixed <see cref="SingletonId"/>) holding the platform fee rate
/// that Payment.Charge applies — see docs/roadmap.md's note on why this was previously a
/// hardcoded constant on Payment and is now admin-configurable. Lazily created with the same
/// default rate Payment.Charge used to hardcode, the first time an admin reads or a payment
/// is captured before any admin has changed it — see AdminPlatformFeeService/PaymentService.
/// </summary>
public sealed class PlatformFeeSettings : Entity
{
    /// <remarks>15% — the same default Payment.Charge previously hardcoded; see
    /// docs/south-africa.md's payment-provider note that this is still a placeholder
    /// pending an actual pricing/business decision.</remarks>
    public const decimal DefaultRate = 0.15m;

    public static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public decimal Rate { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public Guid? UpdatedByAdminUserId { get; private set; }

    private PlatformFeeSettings()
    {
    }

    public static PlatformFeeSettings CreateDefault() => new()
    {
        Id = SingletonId,
        Rate = DefaultRate,
        UpdatedAtUtc = DateTimeOffset.UtcNow,
    };

    public void UpdateRate(decimal rate, Guid adminUserId)
    {
        if (rate is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(rate), rate, "Platform fee rate must be between 0 and 1.");
        }

        Rate = rate;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedByAdminUserId = adminUserId;
    }
}
