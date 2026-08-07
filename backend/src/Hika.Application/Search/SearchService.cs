using Hika.Application.Common.Pagination;
using Hika.Application.Common.Persistence;
using Hika.Application.Search.Dtos;
using Hika.Application.Trips;
using Hika.Application.Trips.Dtos;
using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Search;

public sealed class SearchService(IAppDbContext db) : ISearchService
{
    /// <summary>South Africa runs UTC+2 year-round (no DST) — see docs/south-africa.md.</summary>
    private static readonly TimeSpan SastOffset = TimeSpan.FromHours(2);

    public async Task<PagedResult<SearchTripResponse>> SearchTripsAsync(SearchTripsRequest request, CancellationToken cancellationToken)
    {
        var from = request.From.Trim().ToLowerInvariant();
        var to = request.To.Trim().ToLowerInvariant();

        // Each (TripId, FromSeq, ToSeq) candidate is a stop matching "from" paired with a later
        // stop matching "to" on the same trip — the leg the rider would actually book (see
        // docs/domain-model.md §4's segment-booking design). A trip can produce several matching
        // pairs if its stop names are ambiguous; the shortest span is kept as the most likely leg.
        var candidates = await db.TripStops
            .Where(s => s.RawName.ToLower().Contains(from))
            .Join(
                db.TripStops.Where(s => s.RawName.ToLower().Contains(to)),
                boarding => boarding.TripId,
                alighting => alighting.TripId,
                (boarding, alighting) => new { boarding.TripId, FromSeq = boarding.Sequence, ToSeq = alighting.Sequence })
            .Where(x => x.ToSeq > x.FromSeq)
            .ToListAsync(cancellationToken);

        var bestLegByTrip = candidates
            .GroupBy(x => x.TripId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.ToSeq - x.FromSeq).First());

        if (bestLegByTrip.Count == 0)
        {
            return PagedResult<SearchTripResponse>.Create([], request.Page, request.PageSize, 0);
        }

        var tripIds = bestLegByTrip.Keys.ToList();
        var tripsQuery = db.Trips
            .Include(t => t.Stops)
            .Include(t => t.Segments)
            .Where(t => tripIds.Contains(t.Id) && t.Status == TripStatus.Scheduled);

        if (request.Date is { } date)
        {
            var dayStart = SastDayStartUtc(date);
            var dayEnd = SastDayStartUtc(date.AddDays(1));
            tripsQuery = tripsQuery.Where(t => t.DepartureAtUtc >= dayStart && t.DepartureAtUtc < dayEnd);
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            tripsQuery = tripsQuery.Where(t => t.DepartureAtUtc >= now);
        }

        var trips = await tripsQuery.ToListAsync(cancellationToken);

        var driverIds = trips.Select(t => t.DriverProfileId).Distinct().ToList();
        var driverSummaries = await TripDisplayHelpers.BuildDriverSummariesAsync(db, driverIds, cancellationToken);
        var locations = await TripDisplayHelpers.LoadLocationsAsync(db, trips.SelectMany(t => t.Stops), cancellationToken);

        var results = new List<SearchTripResponse>();
        foreach (var trip in trips)
        {
            if (!bestLegByTrip.TryGetValue(trip.Id, out var leg) || !driverSummaries.TryGetValue(trip.DriverProfileId, out var driver))
            {
                continue;
            }

            var seatsAvailable = SeatsAvailableForLeg(trip, leg.FromSeq, leg.ToSeq);
            if (seatsAvailable < request.Passengers)
            {
                continue;
            }
            if (request.VerifiedOnly && !driver.IsVerifiedDriver)
            {
                continue;
            }
            if (request.MaxPrice is { } maxPrice && trip.PricePerSeat.Amount > maxPrice)
            {
                continue;
            }

            var boardingStop = trip.Stops.First(s => s.Sequence == leg.FromSeq);
            var alightingStop = trip.Stops.First(s => s.Sequence == leg.ToSeq);

            results.Add(new SearchTripResponse
            {
                Id = trip.Id,
                DepartureAtUtc = trip.DepartureAtUtc,
                BoardingStopName = TripDisplayHelpers.ResolveStopName(boardingStop, locations),
                BoardingProvince = boardingStop.Province.ToString(),
                AlightingStopName = TripDisplayHelpers.ResolveStopName(alightingStop, locations),
                AlightingProvince = alightingStop.Province.ToString(),
                TotalSeatsOffered = trip.TotalSeatsOffered,
                SeatsAvailable = seatsAvailable,
                PricePerSeat = trip.PricePerSeat.Amount,
                Driver = driver,
            });
        }

        var sorted = Sort(results, request.Sort);
        var page = sorted.Skip(request.Skip).Take(request.PageSize).ToList();

        return PagedResult<SearchTripResponse>.Create(page, request.Page, request.PageSize, sorted.Count);
    }

    public async Task<IReadOnlyList<LocationResponse>> SearchLocationsAsync(string query, CancellationToken cancellationToken)
    {
        var normalized = query.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
        {
            return [];
        }

        var locations = await db.Locations
            .Where(l => l.Name.ToLower().Contains(normalized))
            .OrderBy(l => l.Name)
            .Take(10)
            .ToListAsync(cancellationToken);

        return locations
            .Select(l => new LocationResponse { Id = l.Id, Name = l.Name, Province = l.Province.ToString(), Type = l.Type.ToString() })
            .ToList();
    }

    public async Task<IReadOnlyList<PopularRouteResponse>> GetPopularRoutesAsync(DateOnly? month, CancellationToken cancellationToken)
    {
        var targetMonth = month ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = SastDayStartUtc(new DateOnly(targetMonth.Year, targetMonth.Month, 1));
        var monthEnd = monthStart.AddMonths(1);

        var trips = await db.Trips
            .Include(t => t.Stops)
            .Where(t => t.Status == TripStatus.Scheduled && t.DepartureAtUtc >= monthStart && t.DepartureAtUtc < monthEnd)
            .ToListAsync(cancellationToken);

        if (trips.Count == 0)
        {
            return [];
        }

        var locations = await TripDisplayHelpers.LoadLocationsAsync(db, trips.SelectMany(t => t.Stops), cancellationToken);

        return trips
            .Select(t => (Origin: TripDisplayHelpers.ResolveStopName(t.Stops[0], locations), Destination: TripDisplayHelpers.ResolveStopName(t.Stops[^1], locations)))
            .GroupBy(r => (r.Origin, r.Destination))
            .Select(g => new PopularRouteResponse { OriginName = g.Key.Origin, DestinationName = g.Key.Destination, TripCount = g.Count() })
            .OrderByDescending(r => r.TripCount)
            .Take(10)
            .ToList();
    }

    private static DateTimeOffset SastDayStartUtc(DateOnly date) =>
        new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), SastOffset).ToUniversalTime();

    private static int SeatsAvailableForLeg(Trip trip, int fromSequence, int toSequence)
    {
        var sequenceByStopId = trip.Stops.ToDictionary(s => s.Id, s => s.Sequence);
        var seats = trip.Segments
            .Where(seg => sequenceByStopId[seg.FromStopId] >= fromSequence && sequenceByStopId[seg.ToStopId] <= toSequence)
            .Select(seg => seg.SeatsAvailable)
            .ToList();

        return seats.Count == 0 ? 0 : seats.Min();
    }

    private static List<SearchTripResponse> Sort(List<SearchTripResponse> results, TripSearchSort sort) => sort switch
    {
        TripSearchSort.Price => results.OrderBy(r => r.PricePerSeat).ToList(),
        TripSearchSort.DriverRating => results.OrderByDescending(r => r.Driver.AverageRating ?? 0).ToList(),
        TripSearchSort.SeatsAvailable => results.OrderByDescending(r => r.SeatsAvailable).ToList(),
        _ => results.OrderBy(r => r.DepartureAtUtc).ToList(),
    };

}
