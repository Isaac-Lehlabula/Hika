using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Pagination;
using Hika.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Admin;

public sealed class AdminAuditLogService(IAppDbContext db) : IAdminAuditLogService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<AuditLogResponse>> GetLogsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => DefaultPageSize, > MaxPageSize => MaxPageSize, _ => pageSize };

        var query = db.AuditLogs.OrderByDescending(a => a.CreatedAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var logs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        if (logs.Count == 0)
        {
            return PagedResult<AuditLogResponse>.Create([], page, pageSize, totalCount);
        }

        var adminIds = logs.Select(a => a.AdminUserId).Distinct().ToList();
        var names = await db.UserProfiles.Where(p => adminIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => $"{p.FirstName} {p.LastName}", cancellationToken);

        var responses = logs.Select(a => new AuditLogResponse
        {
            Id = a.Id,
            AdminUserId = a.AdminUserId,
            AdminName = names.GetValueOrDefault(a.AdminUserId, "Unknown"),
            Action = a.Action,
            EntityType = a.EntityType,
            EntityId = a.EntityId,
            Details = a.Details,
            CreatedAtUtc = a.CreatedAtUtc,
        }).ToList();

        return PagedResult<AuditLogResponse>.Create(responses, page, pageSize, totalCount);
    }
}
