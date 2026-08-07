using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Pagination;
using Hika.Application.Common.Persistence;
using Hika.Application.Payments;
using Hika.Application.Payments.Dtos;
using Hika.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Admin;

public sealed class AdminPaymentService(IAppDbContext db, IPaymentService paymentService, IAuditLogger auditLogger) : IAdminPaymentService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<AdminPaymentSummaryResponse>> GetPaymentsAsync(
        PaymentStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => DefaultPageSize, > MaxPageSize => MaxPageSize, _ => pageSize };

        var query = db.Payments.AsQueryable();
        if (status is not null)
        {
            query = query.Where(p => p.Status == status);
        }

        query = query.OrderByDescending(p => p.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var payments = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var responses = payments.Select(p => new AdminPaymentSummaryResponse
        {
            Id = p.Id,
            BookingId = p.BookingId,
            Amount = p.Amount.Amount,
            PlatformFee = p.PlatformFee.Amount,
            DriverPayoutAmount = p.DriverPayoutAmount.Amount,
            Provider = p.Provider.ToString(),
            ProviderReference = p.ProviderReference,
            Status = p.Status.ToString(),
            CreatedAtUtc = p.CreatedAtUtc,
        }).ToList();

        return PagedResult<AdminPaymentSummaryResponse>.Create(responses, page, pageSize, totalCount);
    }

    public async Task<PaymentResponse> RefundAsync(Guid adminUserId, Guid bookingId, string reason, CancellationToken cancellationToken)
    {
        // IPaymentService.AdminRefundAsync saves its own unit of work (same as the
        // driver-facing RefundAsync it shares logic with), so the audit log entry below is a
        // second, separate save rather than atomic with the refund itself — acceptable here
        // since a missing audit row is a paper-trail gap, not a financial-correctness one.
        var result = await paymentService.AdminRefundAsync(bookingId, reason, cancellationToken);
        auditLogger.Record(adminUserId, "AdminRefund", "Payment", result.Id, reason);
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }
}
