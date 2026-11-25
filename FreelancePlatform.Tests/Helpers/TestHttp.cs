using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Tests.Helpers;

public static class TestHttp
{
    public static HttpContext As(Guid userId) => new DefaultHttpContext
    {
        User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"))
    };
}