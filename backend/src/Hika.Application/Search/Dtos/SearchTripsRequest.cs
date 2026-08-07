namespace Hika.Application.Search.Dtos;

/// <summary>
/// Pickup-distance sorting isn't offered yet — it needs geocoded lat/lng for both the rider's
/// query and every stop, and Location rows are seeded without coordinates today (see
/// docs/south-africa.md's geocoding-provider note). Add it once that lands.
/// </summary>
public enum TripSearchSort
{
    DepartureTime,
    Price,
    DriverRating,
    SeatsAvailable,
}

public sealed record SearchTripsRequest
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private readonly int _page = 1;
    private readonly int _pageSize = DefaultPageSize;

    public required string From { get; init; }

    public required string To { get; init; }

    /// <summary>Null means "any date from now on" rather than "today only".</summary>
    public DateOnly? Date { get; init; }

    public int Passengers { get; init; } = 1;

    public TripSearchSort Sort { get; init; } = TripSearchSort.DepartureTime;

    public bool VerifiedOnly { get; init; }

    public decimal? MaxPrice { get; init; }

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value,
        };
    }

    public int Skip => (Page - 1) * PageSize;
}
