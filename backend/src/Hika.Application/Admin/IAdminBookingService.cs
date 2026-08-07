using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Pagination;
using Hika.Domain.Bookings;

namespace Hika.Application.Admin;

public interface IAdminBookingService
{
    Task<PagedResult<AdminBookingSummaryResponse>> GetBookingsAsync(
        BookingStatus? status, int page, int pageSize, CancellationToken cancellationToken);
}
