namespace Hika.Application.RideRequests.Dtos;

public sealed record RideRequestResponse
{
    public required Guid Id { get; init; }

    public required string OriginRawText { get; init; }

    public required string DestinationRawText { get; init; }

    public required DateOnly TravelDate { get; init; }

    public required int SeatsNeeded { get; init; }

    public decimal? ProposedPricePerSeat { get; init; }

    public required string Status { get; init; }

    /// <summary>Status stays Open in the database (nothing sweeps it) — this reflects whether
    /// its TravelDate has already passed, computed at read time. See RideRequest's remarks.</summary>
    public required bool IsExpired { get; init; }

    public Guid? ClaimedBookingId { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}
