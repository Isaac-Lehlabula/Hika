using Hika.Application.Common.Persistence;
using Hika.Application.Trips.Dtos;
using Hika.Domain.Drivers;
using Hika.Domain.Trips;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Trips;

/// <summary>
/// Batch read-model helpers shared by TripService, SearchService, and BookingService — all
/// three need driver/vehicle/location display data for one or more trips without N+1
/// querying. Extracted here once it was actually duplicated three times (see the project's
/// "don't create abstractions until they earn their keep" guidance).
/// </summary>
internal static class TripDisplayHelpers
{
    public static async Task<Dictionary<Guid, TripDriverSummary>> BuildDriverSummariesAsync(
        IAppDbContext db, IReadOnlyList<Guid> driverUserIds, CancellationToken cancellationToken)
    {
        if (driverUserIds.Count == 0)
        {
            return [];
        }

        var profiles = await db.UserProfiles.Where(p => driverUserIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var driverProfiles = await db.DriverProfiles.Where(d => driverUserIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, cancellationToken);

        var result = new Dictionary<Guid, TripDriverSummary>();
        foreach (var id in driverUserIds)
        {
            if (!profiles.TryGetValue(id, out var profile) || !driverProfiles.TryGetValue(id, out var driverProfile))
            {
                continue;
            }

            result[id] = new TripDriverSummary
            {
                UserId = profile.Id,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                PhotoUrl = profile.PhotoUrl,
                AverageRating = profile.AverageRating,
                CompletedTripCount = profile.CompletedTripCount,
                IsVerifiedDriver = driverProfile.IsVerifiedDriver,
            };
        }

        return result;
    }

    public static async Task<Dictionary<Guid, Location>> LoadLocationsAsync(
        IAppDbContext db, IEnumerable<TripStop> stops, CancellationToken cancellationToken)
    {
        var locationIds = stops.Where(s => s.LocationId.HasValue).Select(s => s.LocationId!.Value).Distinct().ToList();
        if (locationIds.Count == 0)
        {
            return [];
        }

        return await db.Locations.Where(l => locationIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, cancellationToken);
    }

    public static string ResolveStopName(TripStop stop, IReadOnlyDictionary<Guid, Location> locations) =>
        stop.LocationId.HasValue && locations.TryGetValue(stop.LocationId.Value, out var location) ? location.Name : stop.RawName;

    public static TripVehicleSummary ToVehicleSummary(Vehicle vehicle) => new()
    {
        Id = vehicle.Id,
        Make = vehicle.Make,
        Model = vehicle.Model,
        Year = vehicle.Year,
        Color = vehicle.Color,
        IsVerified = vehicle.IsVerified,
        PrimaryPhotoUrl = (vehicle.Photos.FirstOrDefault(p => p.IsPrimary) ?? vehicle.Photos.FirstOrDefault())?.Url,
    };
}
