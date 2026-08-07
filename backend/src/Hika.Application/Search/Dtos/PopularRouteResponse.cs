namespace Hika.Application.Search.Dtos;

/// <summary>Aggregated from real posted-trip activity — never hardcoded, per docs/api-design.md's
/// "Popular Routes" note. Weighting by completed bookings instead of posted trips is a natural
/// upgrade once Bookings (Phase 6) exist.</summary>
public sealed record PopularRouteResponse
{
    public required string OriginName { get; init; }

    public required string DestinationName { get; init; }

    public required int TripCount { get; init; }
}
