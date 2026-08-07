using Hika.Application.Users;
using Hika.Application.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(IUserProfileService userProfileService) : ControllerBase
{
    /// <summary>Public profile — no phone/email, see docs/security.md's PII-minimization rule.</summary>
    [HttpGet("{userId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicUserProfileResponse>> GetPublicProfile(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await userProfileService.GetPublicProfileAsync(userId, cancellationToken);
        return Ok(profile);
    }
}
