using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Pagination;
using Hika.Application.Common.Persistence;
using Hika.Application.Users.Ports;
using Hika.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Admin;

public sealed class AdminUserService(IAppDbContext db, IUserAccountService userAccounts, IAuditLogger auditLogger) : IAdminUserService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<PagedResult<AdminUserSummaryResponse>> GetUsersAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch { < 1 => DefaultPageSize, > MaxPageSize => MaxPageSize, _ => pageSize };

        var query = db.UserProfiles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.FirstName.ToLower().Contains(term) || p.LastName.ToLower().Contains(term));
        }

        query = query.OrderByDescending(p => p.MemberSinceUtc);

        var totalCount = await query.CountAsync(cancellationToken);
        var profiles = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var accounts = (await userAccounts.FindByIdsAsync(profiles.Select(p => p.Id).ToList(), cancellationToken))
            .ToDictionary(a => a.UserId);

        var responses = profiles
            .Where(p => accounts.ContainsKey(p.Id))
            .Select(p => ToResponse(p, accounts[p.Id]))
            .ToList();

        return PagedResult<AdminUserSummaryResponse>.Create(responses, page, pageSize, totalCount);
    }

    public async Task<AdminUserSummaryResponse> SuspendAsync(
        Guid adminUserId, Guid userId, string reason, CancellationToken cancellationToken)
    {
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        profile.Suspend(reason);
        auditLogger.Record(adminUserId, "SuspendUser", nameof(UserProfile), userId, reason);
        await db.SaveChangesAsync(cancellationToken);

        var account = await userAccounts.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);
        return ToResponse(profile, account);
    }

    public async Task<AdminUserSummaryResponse> UnsuspendAsync(Guid adminUserId, Guid userId, CancellationToken cancellationToken)
    {
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        profile.Unsuspend();
        auditLogger.Record(adminUserId, "UnsuspendUser", nameof(UserProfile), userId, null);
        await db.SaveChangesAsync(cancellationToken);

        var account = await userAccounts.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);
        return ToResponse(profile, account);
    }

    private static AdminUserSummaryResponse ToResponse(UserProfile profile, UserAccountSummary account) => new()
    {
        UserId = profile.Id,
        Email = account.Email,
        FirstName = profile.FirstName,
        LastName = profile.LastName,
        EmailVerified = account.EmailConfirmed,
        PhoneVerified = profile.PhoneVerifiedAtUtc is not null,
        IsAdmin = profile.IsAdmin,
        IsSuspended = profile.IsSuspended,
        SuspensionReason = profile.SuspensionReason,
        AverageRating = profile.AverageRating,
        CompletedTripCount = profile.CompletedTripCount,
        MemberSinceUtc = profile.MemberSinceUtc,
    };
}
