namespace Hika.Application.RideRequests.Dtos;

public sealed record ClaimRideRequestRequest
{
    public required Guid TripId { get; init; }

    public required int BoardingStopSequence { get; init; }

    public required int AlightingStopSequence { get; init; }
}
