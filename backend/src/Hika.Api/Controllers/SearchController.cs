using Hika.Application.Search;
using Hika.Application.Search.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
[AllowAnonymous]
public sealed class SearchController(ISearchService searchService) : ControllerBase
{
    [HttpGet("trips")]
    public async Task<IActionResult> SearchTrips([FromQuery] SearchTripsRequest request, CancellationToken cancellationToken)
    {
        var result = await searchService.SearchTripsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("locations")]
    public async Task<ActionResult<IReadOnlyList<LocationResponse>>> SearchLocations(
        [FromQuery] string query, CancellationToken cancellationToken)
    {
        var locations = await searchService.SearchLocationsAsync(query, cancellationToken);
        return Ok(locations);
    }

    [HttpGet("popular-routes")]
    public async Task<ActionResult<IReadOnlyList<PopularRouteResponse>>> GetPopularRoutes(
        [FromQuery] DateOnly? month, CancellationToken cancellationToken)
    {
        var routes = await searchService.GetPopularRoutesAsync(month, cancellationToken);
        return Ok(routes);
    }
}
