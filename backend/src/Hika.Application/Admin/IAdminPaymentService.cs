using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Pagination;
using Hika.Application.Payments.Dtos;
using Hika.Domain.Payments;

namespace Hika.Application.Admin;

public interface IAdminPaymentService
{
    Task<PagedResult<AdminPaymentSummaryResponse>> GetPaymentsAsync(
        PaymentStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    Task<PaymentResponse> RefundAsync(Guid adminUserId, Guid bookingId, string reason, CancellationToken cancellationToken);
}
