using Hika.Domain.Common;

namespace Hika.Domain.Trips;

public enum LocationType
{
    City,
    Town,
    Township,
    Village,
}

/// <summary>
/// Seeded lookup table powering autocomplete (see docs/domain-model.md §11) — a UX aid, not a
/// constraint. A TripStop always also stores RawName/Province so an unlisted village never
/// blocks trip creation, even when it has no matching Location row.
/// </summary>
public sealed class Location : Entity
{
    public string Name { get; private set; }

    public Province Province { get; private set; }

    public LocationType Type { get; private set; }

    public decimal? Latitude { get; private set; }

    public decimal? Longitude { get; private set; }

    private Location()
    {
        Name = string.Empty;
    }

    public Location(string name, Province province, LocationType type, decimal? latitude = null, decimal? longitude = null)
    {
        Name = name;
        Province = province;
        Type = type;
        Latitude = latitude;
        Longitude = longitude;
    }
}
