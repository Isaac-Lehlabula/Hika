using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Pagination;
using Hika.Domain.TrustSafety;

namespace Hika.Application.Admin;

public interface IAdminVerificationService
{
    Task<PagedResult<AdminVerificationResponse>> GetQueueAsync(
        VerificationStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    Task<AdminVerificationResponse> ApproveAsync(Guid adminUserId, Guid verificationId, CancellationToken cancellationToken);

    Task<AdminVerificationResponse> RejectAsync(Guid adminUserId, Guid verificationId, string reason, CancellationToken cancellationToken);
}
