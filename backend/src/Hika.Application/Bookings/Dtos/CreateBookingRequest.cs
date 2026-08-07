namespace Hika.Application.Bookings.Dtos;

public sealed record CreateBookingRequest
{
    public required Guid TripId { get; init; }

    /// <summary>Matches a TripStopResponse.Sequence from the trip detail the rider is booking from.</summary>
    public required int BoardingStopSequence { get; init; }

    public required int AlightingStopSequence { get; init; }

    public required int SeatsRequested { get; init; }
}
