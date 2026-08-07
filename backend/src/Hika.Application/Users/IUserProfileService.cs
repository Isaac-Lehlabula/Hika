using Hika.Application.Users.Dtos;

namespace Hika.Application.Users;

public interface IUserProfileService
{
    Task<UserProfileResponse> GetOwnProfileAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);

    Task<PublicUserProfileResponse> GetPublicProfileAsync(Guid userId, CancellationToken cancellationToken);
}
