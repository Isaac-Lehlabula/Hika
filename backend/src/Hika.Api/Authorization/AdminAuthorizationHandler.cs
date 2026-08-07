using Hika.Api.Common;
using Hika.Application.Common.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Hika.Api.Authorization;

/// <summary>Marker for the "Admin" policy — see AdminAuthorizationHandler for why this is a
/// DB-checked requirement rather than a JWT claim.</summary>
public sealed class AdminRequirement : IAuthorizationRequirement;

/// <summary>
/// Checks UserProfile.IsAdmin fresh from the database on every request, rather than baking an
/// "admin" claim into the JWT at login. Admin status can be revoked (e.g. a compromised staff
/// account) and that needs to take effect immediately, not after the holder's refresh token
/// naturally rotates — an acceptable extra DB read given the admin portal's low request volume
/// compared to the customer-facing API. See docs/admin-portal.md.
/// </summary>
public sealed class AdminAuthorizationHandler(IAppDbContext db) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = context.User.GetUserId();
        var isAdmin = await db.UserProfiles
            .Where(p => p.Id == userId)
            .Select(p => p.IsAdmin)
            .FirstOrDefaultAsync();

        if (isAdmin)
        {
            context.Succeed(requirement);
        }
    }
}
