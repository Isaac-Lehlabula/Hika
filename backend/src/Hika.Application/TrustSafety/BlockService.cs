using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.TrustSafety.Dtos;
using Hika.Domain.TrustSafety;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.TrustSafety;

public sealed class BlockService(IAppDbContext db) : IBlockService
{
    public async Task BlockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken)
    {
        if (blockerUserId == blockedUserId)
        {
            throw new AppValidationException("userId", "You can't block yourself.");
        }

        var alreadyBlocked = await db.Blocks.AnyAsync(
            b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId, cancellationToken);
        if (alreadyBlocked)
        {
            return;
        }

        db.Blocks.Add(Block.Create(blockerUserId, blockedUserId));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnblockAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken)
    {
        var block = await db.Blocks.FirstOrDefaultAsync(
            b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId, cancellationToken);
        if (block is null)
        {
            return;
        }

        db.Blocks.Remove(block);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BlockedUserResponse>> GetMyBlocksAsync(Guid blockerUserId, CancellationToken cancellationToken)
    {
        var blocks = await db.Blocks
            .Where(b => b.BlockerUserId == blockerUserId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (blocks.Count == 0)
        {
            return [];
        }

        var blockedIds = blocks.Select(b => b.BlockedUserId).ToList();
        var profiles = await db.UserProfiles.Where(p => blockedIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);

        var results = new List<BlockedUserResponse>();
        foreach (var block in blocks)
        {
            if (!profiles.TryGetValue(block.BlockedUserId, out var profile))
            {
                continue;
            }

            results.Add(new BlockedUserResponse
            {
                UserId = profile.Id,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                PhotoUrl = profile.PhotoUrl,
                BlockedAtUtc = block.CreatedAtUtc,
            });
        }

        return results;
    }
}
