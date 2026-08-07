using Hika.Application.Common.Exceptions;
using Hika.Application.Common.Persistence;
using Hika.Application.Users.Dtos;
using Hika.Application.Users.Ports;
using Hika.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Hika.Application.Users;

public sealed class UserProfileService(IAppDbContext db, IUserAccountService userAccounts) : IUserProfileService
{
    public async Task<UserProfileResponse> GetOwnProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        var account = await userAccounts.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        return ToResponse(profile, account);
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(
        Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        profile.UpdateName(request.FirstName, request.LastName);
        profile.UpdatePhoto(request.PhotoUrl);
        await db.SaveChangesAsync(cancellationToken);

        var account = await userAccounts.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        return ToResponse(profile, account);
    }

    public async Task<PublicUserProfileResponse> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserProfile), userId);

        return new PublicUserProfileResponse
        {
            UserId = profile.Id,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            PhotoUrl = profile.PhotoUrl,
            MemberSinceUtc = profile.MemberSinceUtc,
            AverageRating = profile.AverageRating,
            CompletedTripCount = profile.CompletedTripCount,
        };
    }

    private static UserProfileResponse ToResponse(UserProfile profile, UserAccountSummary account) => new()
    {
        UserId = profile.Id,
        Email = account.Email,
        EmailVerified = account.EmailConfirmed,
        FirstName = profile.FirstName,
        LastName = profile.LastName,
        PhotoUrl = profile.PhotoUrl,
        PhoneNumber = profile.PhoneNumber,
        PhoneVerified = profile.PhoneVerifiedAtUtc is not null,
        MemberSinceUtc = profile.MemberSinceUtc,
        AverageRating = profile.AverageRating,
        CompletedTripCount = profile.CompletedTripCount,
    };
}
