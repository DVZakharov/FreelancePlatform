using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FreelancePlatform.Application.Common.Tokens;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Auth.RefreshToken;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Auth;

public class RefreshTokenTests
{
    private readonly TestDb _testDb = new();
    private static readonly JwtSettings  JwtSettings = new()
    {
        Audience = "test",
        AccessTokenExpiryMinutes = 30,
        Issuer = "test",
        RefreshTokenExpiryDays = 14,
        Secret = "unit-test-secret-at-least-32-characters!!"
    };

    private static readonly JwtTokenGenerator Jwt = new(Options.Create(JwtSettings));
    private static readonly RefreshTokenGenerator Refresh = new();

    private async Task SeedTokensAsync(params (string Token, DateTime Expires, DateTime? Revoked)[] tokens)
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser();
        db.Users.Add(user);
        foreach (var (token, expires, revoked) in tokens)
            db.RefreshTokens.Add(new RefreshTokenEntity
            {
                UserId = user.Id, Token = token, ExpiresAt = expires, RevokedAt = revoked
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Timeout = 10_000)]
    public async Task Refresh_ActiveToken_RotatesAndIssuesNewPair()
    {
        await SeedTokensAsync(("old", DateTime.UtcNow.AddDays(1), null));

        var response = await _testDb.RunAsync(db => RefreshTokenEndpoint.Handle(
            new RefreshTokenRequest("old"), db, Jwt, Refresh, JwtSettings, TestContext.Current.CancellationToken));

        var result = ResultAssert.Unwrap<LoginResponse>(response);
        Assert.Equal(200, result.Code);
        Assert.NotEqual("old", result.Value!.RefreshToken);

        await using var check = _testDb.NewContext();
        var old = await check.RefreshTokens.SingleAsync(t => t.Token == "old", TestContext.Current.CancellationToken);
        Assert.NotNull(old.RevokedAt);
        Assert.Equal(result.Value.RefreshToken, old.ReplacedByToken);
        Assert.True((await check.RefreshTokens.SingleAsync(t => t.Token == result.Value.RefreshToken, TestContext.Current.CancellationToken)).IsActive);
    }

    [Fact(Timeout = 10_000)]
    public async Task Refresh_UnknownToken_Returns403()
    {
        await SeedTokensAsync(("old", DateTime.UtcNow.AddDays(1), null));

        var response = await _testDb.RunAsync(db => RefreshTokenEndpoint.Handle(
            new RefreshTokenRequest("no-such-token"), db, Jwt, Refresh, JwtSettings, TestContext.Current.CancellationToken));

        Assert.Equal(401, ResultAssert.Unwrap<LoginResponse>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Refresh_ReusedRevokedToken_Returns403_AndRevokesAllUserTokens()
    {
        await SeedTokensAsync(
            ("stolen", DateTime.UtcNow.AddDays(1), DateTime.UtcNow.AddMinutes(-5)),
            ("other-device", DateTime.UtcNow.AddDays(1), null));

        var response = await _testDb.RunAsync(db => RefreshTokenEndpoint.Handle(
            new RefreshTokenRequest("stolen"), db, Jwt, Refresh, JwtSettings, TestContext.Current.CancellationToken));

        Assert.Equal(401, ResultAssert.Unwrap<LoginResponse>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.All(check.RefreshTokens, t => Assert.NotNull(t.RevokedAt));
    }

    [Fact(Timeout = 10_000)]
    public async Task Refresh_RotatedTokenReplayed_KillsTheNewToken()
    {
        await SeedTokensAsync(("old", DateTime.UtcNow.AddDays(1), null));

        var first = ResultAssert.Unwrap<LoginResponse>(await _testDb.RunAsync(db => RefreshTokenEndpoint.Handle(
            new RefreshTokenRequest("old"), db, Jwt, Refresh, JwtSettings, TestContext.Current.CancellationToken)));

        var replay = await _testDb.RunAsync(db => RefreshTokenEndpoint.Handle(
            new RefreshTokenRequest("old"), db, Jwt, Refresh, JwtSettings, TestContext.Current.CancellationToken));

        Assert.Equal(401, ResultAssert.Unwrap<LoginResponse>(replay).Code);

        await using var check = _testDb.NewContext();
        var newToken = await check.RefreshTokens.SingleAsync(t => t.Token == first.Value!.RefreshToken, TestContext.Current.CancellationToken);
        Assert.False(newToken.IsActive);
    }
    
    [Fact(Timeout = 10_000)]
    public async Task Refresh_ExpiredToken_Returns401_AndKeepsOtherSessions()
    {
        await SeedTokensAsync(
            ("expired", DateTime.UtcNow.AddDays(-1), null),
            ("other-device", DateTime.UtcNow.AddDays(1), null));

        var response = await _testDb.RunAsync(db => RefreshTokenEndpoint.Handle(
            new RefreshTokenRequest("expired"), db, Jwt, Refresh, JwtSettings, TestContext.Current.CancellationToken));

        Assert.Equal(401, ResultAssert.Unwrap<LoginResponse>(response).Code);

        await using var check = _testDb.NewContext();
        var other = await check.RefreshTokens.SingleAsync(
            t => t.Token == "other-device", TestContext.Current.CancellationToken);
        Assert.Null(other.RevokedAt);
    }
}