using Hika.Application.Search;
using Hika.Domain.Common;
using Hika.Domain.Trips;
using Hika.UnitTests.TestSupport;
using Shouldly;

namespace Hika.UnitTests.Application.Search;

// SearchTripsAsync and GetPopularRoutesAsync both materialize Trip entities, which carry a
// required Money ComplexProperty the EF Core 10 InMemory provider can't shape a query for (the
// same limitation documented in TripServiceTests.cs) — their coverage lives in
// Hika.IntegrationTests/Search/SearchEndpointsTests.cs against real Postgres instead.
// SearchLocationsAsync only touches Location, which has no ComplexProperty, so it's safe here.
public class SearchServiceTests
{
    private readonly InMemoryAppDbContext _db = new();
    private readonly SearchService _sut;

    public SearchServiceTests()
    {
        _sut = new SearchService(_db);
        _db.Locations.AddRange(
            new Location("Johannesburg", Province.Gauteng, LocationType.City),
            new Location("Polokwane", Province.Limpopo, LocationType.City),
            new Location("Cape Town", Province.WesternCape, LocationType.City));
        _db.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task SearchLocationsAsync_MatchesCaseInsensitiveSubstring()
    {
        var results = await _sut.SearchLocationsAsync("johann", CancellationToken.None);

        results.ShouldHaveSingleItem();
        results[0].Name.ShouldBe("Johannesburg");
    }

    [Fact]
    public async Task SearchLocationsAsync_EmptyQuery_ReturnsEmpty()
    {
        var results = await _sut.SearchLocationsAsync("   ", CancellationToken.None);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchLocationsAsync_NoMatch_ReturnsEmpty()
    {
        var results = await _sut.SearchLocationsAsync("mumbai", CancellationToken.None);

        results.ShouldBeEmpty();
    }
}
