namespace Hika.Application.Admin.Dtos;

public sealed record PlatformFeeResponse
{
    /// <summary>0.15 for 15% — the same fraction Payment.Charge multiplies the fare by.</summary>
    public required decimal Rate { get; init; }

    public required DateTimeOffset UpdatedAtUtc { get; init; }

    public Guid? UpdatedByAdminUserId { get; init; }
}
