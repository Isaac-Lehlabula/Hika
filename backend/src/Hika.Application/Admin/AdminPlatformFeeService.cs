using Hika.Application.Admin.Dtos;
using Hika.Application.Common.Persistence;
using Hika.Domain.Admin;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Admin;

public sealed class AdminPlatformFeeService(IAppDbContext db, IAuditLogger auditLogger) : IAdminPlatformFeeService
{
    public async Task<PlatformFeeResponse> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return ToResponse(settings);
    }

    public async Task<PlatformFeeResponse> UpdateAsync(Guid adminUserId, decimal rate, CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateAsync(cancellationToken);

        settings.UpdateRate(rate, adminUserId);
        auditLogger.Record(adminUserId, "UpdatePlatformFee", nameof(PlatformFeeSettings), settings.Id, $"Rate {rate:P1}");
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(settings);
    }

    private async Task<PlatformFeeSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await db.PlatformFeeSettings.FirstOrDefaultAsync(s => s.Id == PlatformFeeSettings.SingletonId, cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = PlatformFeeSettings.CreateDefault();
        db.PlatformFeeSettings.Add(settings);
        return settings;
    }

    private static PlatformFeeResponse ToResponse(PlatformFeeSettings settings) => new()
    {
        Rate = settings.Rate,
        UpdatedAtUtc = settings.UpdatedAtUtc,
        UpdatedByAdminUserId = settings.UpdatedByAdminUserId,
    };
}
