using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Hika.Api.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The authenticated principal has no 'sub' claim.");

        return Guid.Parse(subject);
    }
}
