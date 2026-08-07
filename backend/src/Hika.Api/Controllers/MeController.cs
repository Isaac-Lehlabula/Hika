using Hika.Api.Common;
using Hika.Application.Users;
using Hika.Application.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/users/me")]
[Authorize]
public sealed class MeController(IUserProfileService userProfileService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserProfileResponse>> GetOwnProfile(CancellationToken cancellationToken)
    {
        var profile = await userProfileService.GetOwnProfileAsync(User.GetUserId(), cancellationToken);
        return Ok(profile);
    }

    [HttpPut]
    public async Task<ActionResult<UserProfileResponse>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await userProfileService.UpdateProfileAsync(User.GetUserId(), request, cancellationToken);
        return Ok(profile);
    }
}
