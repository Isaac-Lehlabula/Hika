using Hika.Domain.Common;
using Hika.Domain.Trips;

namespace Hika.Infrastructure.Persistence;

/// <summary>
/// Seeds the Location lookup table with major SA cities/towns/townships/villages so
/// autocomplete has something to match against from day one — see docs/domain-model.md §11.
/// Deliberately not exhaustive; a driver can always fall back to free text (TripStop.RawName)
/// for anywhere not listed here.
/// </summary>
public static class LocationSeedData
{
    public static IReadOnlyList<Location> Build() =>
    [
        new("Johannesburg", Province.Gauteng, LocationType.City),
        new("Pretoria", Province.Gauteng, LocationType.City),
        new("Midrand", Province.Gauteng, LocationType.City),
        new("Soweto", Province.Gauteng, LocationType.Township),
        new("Mamelodi", Province.Gauteng, LocationType.Township),
        new("Rustenburg", Province.NorthWest, LocationType.Town),
        new("Mahikeng", Province.NorthWest, LocationType.Town),
        new("Klerksdorp", Province.NorthWest, LocationType.Town),
        new("Polokwane", Province.Limpopo, LocationType.City),
        new("Giyani", Province.Limpopo, LocationType.Town),
        new("Mokopane", Province.Limpopo, LocationType.Town),
        new("Tzaneen", Province.Limpopo, LocationType.Town),
        new("Makhado", Province.Limpopo, LocationType.Town),
        new("Thohoyandou", Province.Limpopo, LocationType.Town),
        new("Musina", Province.Limpopo, LocationType.Town),
        new("Mbombela", Province.Mpumalanga, LocationType.City),
        new("Witbank", Province.Mpumalanga, LocationType.Town),
        new("Secunda", Province.Mpumalanga, LocationType.Town),
        new("Durban", Province.KwaZuluNatal, LocationType.City),
        new("Pietermaritzburg", Province.KwaZuluNatal, LocationType.City),
        new("Nongoma", Province.KwaZuluNatal, LocationType.Town),
        new("Newcastle", Province.KwaZuluNatal, LocationType.Town),
        new("Richards Bay", Province.KwaZuluNatal, LocationType.Town),
        new("Ulundi", Province.KwaZuluNatal, LocationType.Town),
        new("Cape Town", Province.WesternCape, LocationType.City),
        new("Stellenbosch", Province.WesternCape, LocationType.Town),
        new("George", Province.WesternCape, LocationType.Town),
        new("Mthatha", Province.EasternCape, LocationType.Town),
        new("Gqeberha", Province.EasternCape, LocationType.City),
        new("East London", Province.EasternCape, LocationType.City),
        new("Queenstown", Province.EasternCape, LocationType.Town),
        new("Bloemfontein", Province.FreeState, LocationType.City),
        new("Welkom", Province.FreeState, LocationType.Town),
        new("Bethlehem", Province.FreeState, LocationType.Town),
        new("Kimberley", Province.NorthernCape, LocationType.City),
        new("Upington", Province.NorthernCape, LocationType.Town),
    ];
}
