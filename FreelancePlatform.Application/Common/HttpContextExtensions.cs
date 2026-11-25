using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FreelancePlatform.Application.Common;

public static class HttpContextExtensions
{
    public static Guid GetCurrentUserId(this HttpContext ctx)
    {
        var claim = ctx.User.FindFirst(JwtRegisteredClaimNames.Sub)
                    ?? ctx.User.FindFirst(ClaimTypes.NameIdentifier);

        if (claim is null)
            throw new UnauthorizedAccessException("User is not authenticated");

        return Guid.Parse(claim.Value);
    }
}