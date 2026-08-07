using Hika.Application.TrustSafety.Dtos;

namespace Hika.Application.TrustSafety;

public interface IBlockService
{
    Task BlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken);

    Task UnblockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BlockedUserResponse>> GetMyBlocksAsync(Guid blockerUserId, CancellationToken cancellationToken);
}
