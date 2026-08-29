namespace Hika.Application.RideRequests.Dtos;

public sealed record CreateRideRequestRequest
{
    public required string Origin { get; init; }

    public required string Destination { get; init; }

    public required DateOnly TravelDate { get; init; }

    public required int SeatsNeeded { get; init; }

    public decimal? ProposedPricePerSeat { get; init; }
}
