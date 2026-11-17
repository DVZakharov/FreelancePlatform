using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Auth.Login;
using FreelancePlatform.Application.Features.Auth.Logout;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Auth;

public class LoginTests
{
    private readonly TestDb _testDb = new();

    private async Task<UserEntity> SeedUserAsync(UserRole role = UserRole.Client)
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser(role: role);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(TestAuth.StrongPassword);
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    private Task<Microsoft.AspNetCore.Http.IResult> LoginAsync(string email, string password) =>
        _testDb.RunAsync(db => LoginEndpoint.Handle(
            new LoginRequest(password, email), TestAuth.Http("browser/1.0", "192.168.0.5"), db, TestAuth.Tokens(db), TestContext.Current.CancellationToken));

    [Fact(Timeout = 10_000)]
    public async Task Login_Valid_ReturnsTokens_AndStoresSession()
    {
        var user = await SeedUserAsync();

        var response = await LoginAsync(user.Email, TestAuth.StrongPassword);

        var result = ResultAssert.Unwrap<LoginResponse>(response);
        Assert.Equal(200, result.Code);
        Assert.Equal(user.Id, result.Value!.UserId);

        await using var check = _testDb.NewContext();
        var session = await check.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(result.Value.RefreshToken, session.Token);
        Assert.Equal("browser/1.0", session.UserAgent);
        Assert.Equal("192.168.0.5", session.IpAddress);
        Assert.True(session.IsActive);
    }

    [Fact(Timeout = 10_000)]
    public async Task Login_TwiceFromSameDevice_CreatesTwoIndependentSessions()
    {
        var user = await SeedUserAsync();

        await LoginAsync(user.Email, TestAuth.StrongPassword);
        await LoginAsync(user.Email, TestAuth.StrongPassword);

        await using var check = _testDb.NewContext();
        var tokens = await check.RefreshTokens.Select(t => t.Token).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, tokens.Count);
        Assert.Equal(2, tokens.Distinct().Count());
    }

    [Fact]
    public async Task Login_AccessToken_ContainsUserIdAndOneRoleClaimPerFlag()
    {
        var user = await SeedUserAsync(UserRole.Client | UserRole.Freelancer);

        var result = ResultAssert.Unwrap<LoginResponse>(await LoginAsync(user.Email, TestAuth.StrongPassword));

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Value!.AccessToken);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        var roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").Select(c => c.Value).ToList();
        Assert.Equal(new[] { "Client", "Freelancer" }, roles.OrderBy(r => r).ToArray());
    }

    [Fact(Timeout = 10_000)]
    public async Task Login_WrongPasswordAndUnknownEmail_AreIndistinguishable()
    {
        var user = await SeedUserAsync();

        var wrongPassword = ResultAssert.Unwrap<LoginResponse>(await LoginAsync(user.Email, "Wrong!Pass1"));
        var unknownEmail = ResultAssert.Unwrap<LoginResponse>(await LoginAsync("nobody@test.local", TestAuth.StrongPassword));

        Assert.Equal(401, wrongPassword.Code);
        Assert.Equal(401, unknownEmail.Code);
        Assert.Equal(wrongPassword.Message, unknownEmail.Message);

        await using var check = _testDb.NewContext();
        Assert.Equal(0, await check.RefreshTokens.CountAsync(TestContext.Current.CancellationToken));
    }

    // ---- Logout ----

    private async Task<string> SeedSessionAsync(Guid userId, DateTime? revokedAt = null)
    {
        await using var db = _testDb.NewContext();
        var token = Guid.NewGuid().ToString();
        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            UserId = userId, Token = token, ExpiresAt = DateTime.UtcNow.AddDays(1), RevokedAt = revokedAt
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return token;
    }

    [Fact(Timeout = 10_000)]
    public async Task Logout_RevokesTheGivenToken()
    {
        var user = await SeedUserAsync();
        var token = await SeedSessionAsync(user.Id);

        var response = await _testDb.RunAsync(db => LogoutEndpoint.Handle(new LogoutRequest(token), db, TestContext.Current.CancellationToken));

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.NotNull((await check.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken)).RevokedAt);
    }

    [Fact(Timeout = 10_000)]
    public async Task Logout_UnknownToken_StillReturns200()
    {
        var response = await _testDb.RunAsync(db => LogoutEndpoint.Handle(new LogoutRequest("nope"), db, TestContext.Current.CancellationToken));

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Logout_AlreadyRevoked_KeepsOriginalRevokedAt()
    {
        var user = await SeedUserAsync();
        var revokedAt = DateTime.UtcNow.AddHours(-3);
        var token = await SeedSessionAsync(user.Id, revokedAt);

        await _testDb.RunAsync(db => LogoutEndpoint.Handle(new LogoutRequest(token), db, TestContext.Current.CancellationToken));

        await using var check = _testDb.NewContext();
        Assert.Equal(revokedAt, (await check.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken)).RevokedAt);
    }
}