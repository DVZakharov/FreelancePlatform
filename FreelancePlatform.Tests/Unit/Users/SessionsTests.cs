using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Users.GetActiveSessions;
using FreelancePlatform.Application.Features.Users.RevokeAllSessions;
using FreelancePlatform.Application.Features.Users.RevokeSession;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Users;

public class SessionsTests
{
    private readonly TestDb _testDb = new();

    private async Task<Guid> SeedUserAsync()
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser();
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user.Id;
    }

    private async Task<Guid> SeedSessionAsync(
        Guid userId, DateTime? expiresAt = null, DateTime? revokedAt = null, DateTime? lastUsedAt = null)
    {
        await using var db = _testDb.NewContext();
        var session = new RefreshTokenEntity
        {
            UserId = userId,
            Token = Guid.NewGuid().ToString(),
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
            RevokedAt = revokedAt,
            LastUsedAt = lastUsedAt
        };
        db.RefreshTokens.Add(session);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return session.Id;
    }

    // ---- GetActiveSessions ----

    [Fact(Timeout = 10_000)]
    public async Task GetActive_ReturnsOnlyOwnActiveSessions_NewestFirst()
    {
        var me = await SeedUserAsync();
        var other = await SeedUserAsync();

        var older = await SeedSessionAsync(me, lastUsedAt: DateTime.UtcNow.AddHours(-5));
        var newer = await SeedSessionAsync(me, lastUsedAt: DateTime.UtcNow.AddHours(-1));
        await SeedSessionAsync(me, revokedAt: DateTime.UtcNow);                        // отозвана
        await SeedSessionAsync(me, expiresAt: DateTime.UtcNow.AddMinutes(-1));         // истекла
        await SeedSessionAsync(other);                                                 // чужая

        var response = await _testDb.RunAsync(db => GetActiveSessionsEndpoint.Handle(TestHttp.As(me), db, TestContext.Current.CancellationToken));

        var result = ResultAssert.Unwrap<ListSessionsDto>(response);
        Assert.Equal(200, result.Code);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(new[] { newer, older }, result.Value.Sessions.Select(s => s.Id).ToArray());
    }

    // ---- RevokeSession ----

    private Task<Microsoft.AspNetCore.Http.IResult> RevokeAsync(Guid userId, string id) =>
        _testDb.RunAsync(db => RevokeSessionEndpoint.Handle(id, TestHttp.As(userId), db, TestContext.Current.CancellationToken));

    [Fact(Timeout = 10_000)]
    public async Task Revoke_OwnSession_SetsRevokedAt()
    {
        var me = await SeedUserAsync();
        var session = await SeedSessionAsync(me);

        var response = await RevokeAsync(me, session.ToString());

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.NotNull((await check.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken)).RevokedAt);
    }

    [Fact(Timeout = 10_000)]
    public async Task Revoke_SomeoneElsesSession_Returns403_AndKeepsItActive()
    {
        var me = await SeedUserAsync();
        var other = await SeedUserAsync();
        var session = await SeedSessionAsync(other);

        var response = await RevokeAsync(me, session.ToString());

        Assert.Equal(403, ResultAssert.Unwrap<object>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Null((await check.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken)).RevokedAt);
    }

    [Fact]
    public async Task Revoke_UnknownSession_Returns404()
    {
        var me = await SeedUserAsync();

        var response = await RevokeAsync(me, Guid.NewGuid().ToString());

        Assert.Equal(404, ResultAssert.Unwrap<object>(response).Code);
    }

    [Fact]
    public async Task Revoke_InvalidId_Returns400()
    {
        var me = await SeedUserAsync();

        var response = await RevokeAsync(me, "not-a-guid");

        Assert.Equal(400, ResultAssert.Unwrap<object>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Revoke_AlreadyRevoked_KeepsOriginalTimestamp()
    {
        var me = await SeedUserAsync();
        var revokedAt = DateTime.UtcNow.AddHours(-2);
        var session = await SeedSessionAsync(me, revokedAt: revokedAt);

        var response = await RevokeAsync(me, session.ToString());

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal(revokedAt, (await check.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken)).RevokedAt);
    }

    // ---- RevokeAllSessions ----

    [Fact(Timeout = 10_000)]
    public async Task RevokeAll_RevokesOnlyMySessions()
    {
        var me = await SeedUserAsync();
        var other = await SeedUserAsync();
        await SeedSessionAsync(me);
        await SeedSessionAsync(me);
        await SeedSessionAsync(other);

        var response = await _testDb.RunAsync(db => RevokeAllSessionsEndpoint.Handle(TestHttp.As(me), db, TestContext.Current.CancellationToken));

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.All(await check.RefreshTokens.Where(t => t.UserId == me).ToListAsync(TestContext.Current.CancellationToken), t => Assert.NotNull(t.RevokedAt));
        Assert.All(await check.RefreshTokens.Where(t => t.UserId == other).ToListAsync(TestContext.Current.CancellationToken), t => Assert.Null(t.RevokedAt));
    }
}