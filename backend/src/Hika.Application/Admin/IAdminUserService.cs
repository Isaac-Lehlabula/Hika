using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Pagination;

namespace Hika.Application.Admin;

public interface IAdminUserService
{
    Task<PagedResult<AdminUserSummaryResponse>> GetUsersAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken);

    Task<AdminUserSummaryResponse> SuspendAsync(Guid adminUserId, Guid userId, string reason, CancellationToken cancellationToken);

    Task<AdminUserSummaryResponse> UnsuspendAsync(Guid adminUserId, Guid userId, CancellationToken cancellationToken);
}
