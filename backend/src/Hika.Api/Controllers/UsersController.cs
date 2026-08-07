using Hika.Api.Common;
using Hika.Application.TrustSafety;
using Hika.Application.TrustSafety.Dtos;
using Hika.Application.Users;
using Hika.Application.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hika.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(IUserProfileService userProfileService, IEmergencyContactService emergencyContactService)
    : ControllerBase
{
    /// <summary>Public profile — no phone/email, see docs/security.md's PII-minimization rule.</summary>
    [HttpGet("{userId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicUserProfileResponse>> GetPublicProfile(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await userProfileService.GetPublicProfileAsync(userId, cancellationToken);
        return Ok(profile);
    }

    [HttpGet("me/emergency-contacts")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<EmergencyContactResponse>>> GetMyEmergencyContacts(CancellationToken cancellationToken)
    {
        var contacts = await emergencyContactService.GetMyContactsAsync(User.GetUserId(), cancellationToken);
        return Ok(contacts);
    }

    [HttpPost("me/emergency-contacts")]
    [Authorize]
    public async Task<ActionResult<EmergencyContactResponse>> CreateEmergencyContact(
        EmergencyContactRequest request, CancellationToken cancellationToken)
    {
        var contact = await emergencyContactService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return Ok(contact);
    }

    [HttpPut("me/emergency-contacts/{contactId:guid}")]
    [Authorize]
    public async Task<ActionResult<EmergencyContactResponse>> UpdateEmergencyContact(
        Guid contactId, EmergencyContactRequest request, CancellationToken cancellationToken)
    {
        var contact = await emergencyContactService.UpdateAsync(User.GetUserId(), contactId, request, cancellationToken);
        return Ok(contact);
    }

    [HttpDelete("me/emergency-contacts/{contactId:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteEmergencyContact(Guid contactId, CancellationToken cancellationToken)
    {
        await emergencyContactService.DeleteAsync(User.GetUserId(), contactId, cancellationToken);
        return NoContent();
    }
}
