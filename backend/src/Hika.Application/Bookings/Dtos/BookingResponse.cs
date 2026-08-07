using Hika.Application.Trips.Dtos;

namespace Hika.Application.Bookings.Dtos;

public sealed record BookingTripSummary
{
    public required Guid TripId { get; init; }

    public required DateTimeOffset DepartureAtUtc { get; init; }

    public required string OriginName { get; init; }

    public required string DestinationName { get; init; }

    public required TripDriverSummary Driver { get; init; }

    public required TripVehicleSummary Vehicle { get; init; }
}

public sealed record BookingPassengerSummary
{
    public required Guid UserId { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public string? PhotoUrl { get; init; }

    public string? PhoneNumber { get; init; }
}

public sealed record BookingResponse
{
    public required Guid Id { get; init; }

    public required string Status { get; init; }

    public required BookingTripSummary Trip { get; init; }

    public required int BoardingStopSequence { get; init; }

    public required string BoardingStopName { get; init; }

    public required int AlightingStopSequence { get; init; }

    public required string AlightingStopName { get; init; }

    public required int SeatsRequested { get; init; }

    public required decimal TotalPrice { get; init; }

    public required BookingPassengerSummary Passenger { get; init; }

    public required DateTimeOffset RequestedAtUtc { get; init; }

    public DateTimeOffset? RespondedAtUtc { get; init; }

    public DateTimeOffset? CancelledAtUtc { get; init; }

    public string? CancellationReason { get; init; }
}
