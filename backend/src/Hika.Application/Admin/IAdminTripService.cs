using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Pagination;
using Hika.Domain.Trips;

namespace Hika.Application.Admin;

public interface IAdminTripService
{
    Task<PagedResult<AdminTripSummaryResponse>> GetTripsAsync(
        TripStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    Task<AdminTripSummaryResponse> RemoveAsync(Guid adminUserId, Guid tripId, string reason, CancellationToken cancellationToken);
}
