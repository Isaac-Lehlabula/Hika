using Hika.Application.Common.Pagination;
using Hika.Application.Search.Dtos;

namespace Hika.Application.Search;

public interface ISearchService
{
    Task<PagedResult<SearchTripResponse>> SearchTripsAsync(SearchTripsRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<LocationResponse>> SearchLocationsAsync(string query, CancellationToken cancellationToken);

    Task<IReadOnlyList<PopularRouteResponse>> GetPopularRoutesAsync(DateOnly? month, CancellationToken cancellationToken);
}
