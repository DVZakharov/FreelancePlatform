using FreelancePlatform.Application.Common.Interfaces;
using FreelancePlatform.Application.Common.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FreelancePlatform.Tests.Helpers;

public static class TestAuth
{
    public static readonly JwtSettings Settings = new()
    {
        Audience = "test",
        Issuer = "test",
        AccessTokenExpiryMinutes = 30,
        RefreshTokenExpiryDays = 14,
        Secret = "unit-test-secret-at-least-32-characters!!"
    };

    public const string StrongPassword = "Str0ng!Pass";

    // Настоящий TokenService поверх той же in-memory БД: токены реально сохраняются
    public static ITokenService Tokens(IApplicationDbContext db) =>
        new TokenService(db, new JwtTokenGenerator(Options.Create(Settings)), new RefreshTokenGenerator(), Options.Create(Settings));

    public static HttpContext Http(string userAgent = "xunit-agent", string ip = "10.0.0.1")
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.UserAgent = userAgent;
        ctx.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return ctx;
    }
}