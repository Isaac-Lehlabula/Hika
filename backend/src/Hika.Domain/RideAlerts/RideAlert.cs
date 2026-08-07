using Hika.Domain.Common;

namespace Hika.Domain.RideAlerts;

public enum RideAlertStatus
{
    Active,
    Fulfilled,
    Cancelled,
}

/// <summary>
/// "Notify me when someone posts JHB → Giyani for 20 Dec." Matched against free text the same
/// way search matches a trip's stops (see docs/domain-model.md §8) — no LocationId reference,
/// consistent with how trip posting/search both already work on RawName substrings today.
/// </summary>
public sealed class RideAlert : AuditableEntity
{
    public Guid UserId { get; private set; }

    public string OriginRawText { get; private set; }

    public string DestinationRawText { get; private set; }

    /// <summary>Null means "any date" rather than a specific day.</summary>
    public DateOnly? TravelDate { get; private set; }

    public RideAlertStatus Status { get; private set; }

    private RideAlert()
    {
        OriginRawText = string.Empty;
        DestinationRawText = string.Empty;
    }

    public static RideAlert Create(Guid userId, string originRawText, string destinationRawText, DateOnly? travelDate) => new()
    {
        UserId = userId,
        OriginRawText = originRawText,
        DestinationRawText = destinationRawText,
        TravelDate = travelDate,
        Status = RideAlertStatus.Active,
    };

    /// <summary>Fires once per matching trip, then the alert is done — a rider who wants to
    /// keep watching a route creates a new alert. Simpler than tracking "already notified for
    /// which trips" on a still-Active alert, and matches the product framing ("notify me").</summary>
    public void MarkFulfilled()
    {
        if (Status != RideAlertStatus.Active)
        {
            throw new InvalidOperationException($"Cannot fulfill a {Status} alert.");
        }

        Status = RideAlertStatus.Fulfilled;
    }

    public void Cancel()
    {
        if (Status != RideAlertStatus.Active)
        {
            throw new InvalidOperationException($"Cannot cancel a {Status} alert.");
        }

        Status = RideAlertStatus.Cancelled;
    }
}
