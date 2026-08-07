using Hika.Application.Admin.Dtos;

namespace Hika.Application.Admin;

public interface IAdminPlatformFeeService
{
    Task<PlatformFeeResponse> GetAsync(CancellationToken cancellationToken);

    Task<PlatformFeeResponse> UpdateAsync(Guid adminUserId, decimal rate, CancellationToken cancellationToken);
}
