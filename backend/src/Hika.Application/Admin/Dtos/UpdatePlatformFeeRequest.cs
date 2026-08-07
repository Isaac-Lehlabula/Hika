namespace Hika.Application.Admin.Dtos;

public sealed record UpdatePlatformFeeRequest
{
    /// <summary>0.15 for 15% — validated to [0, 1] by both the validator and
    /// PlatformFeeSettings.UpdateRate.</summary>
    public required decimal Rate { get; init; }
}
