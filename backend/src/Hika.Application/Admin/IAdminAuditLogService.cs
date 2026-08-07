using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Pagination;

namespace Hika.Application.Admin;

public interface IAdminAuditLogService
{
    Task<PagedResult<AuditLogResponse>> GetLogsAsync(int page, int pageSize, CancellationToken cancellationToken);
}
