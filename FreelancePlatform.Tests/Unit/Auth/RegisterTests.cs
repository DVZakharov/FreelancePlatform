using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Auth.Register;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Auth;

public class RegisterTests
{
    private readonly TestDb _testDb = new();

    private Task<Microsoft.AspNetCore.Http.IResult> RegisterAsync(RegisterRequest request) =>
        _testDb.RunAsync(db => RegisterEndpoint.Handle(
            request, db, TestAuth.Tokens(db), TestAuth.Http(), new RegisterValidator(), TestContext.Current.CancellationToken));

    [Fact(Timeout = 10_000)]
    public async Task Register_Valid_CreatesClientWithHashedPassword_AndIssuesTokens()
    {
        var response = await RegisterAsync(new RegisterRequest("new@test.local", TestAuth.StrongPassword, "Иван Иванов"));

        var result = ResultAssert.Unwrap<LoginResponse>(response);
        Assert.Equal(200, result.Code);
        Assert.Equal("new@test.local", result.Value!.Email);
        Assert.False(string.IsNullOrEmpty(result.Value.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.Value.RefreshToken));

        await using var check = _testDb.NewContext();
        var user = await check.Users.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(UserRole.Client, user.Role);
        Assert.Equal(0m, user.Balance);
        Assert.NotEqual(TestAuth.StrongPassword, user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(TestAuth.StrongPassword, user.PasswordHash));

        var session = await check.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(user.Id, session.UserId);
        Assert.Equal(result.Value.RefreshToken, session.Token);
        Assert.Equal("xunit-agent", session.UserAgent);
        Assert.Equal("10.0.0.1", session.IpAddress);
    }

    [Fact(Timeout = 10_000)]
    public async Task Register_DuplicateEmail_Returns409_AndCreatesNothing()
    {
        await RegisterAsync(new RegisterRequest("dup@test.local", TestAuth.StrongPassword, "Первый"));

        var response = await RegisterAsync(new RegisterRequest("dup@test.local", TestAuth.StrongPassword, "Второй"));

        Assert.Equal(409, ResultAssert.Unwrap<LoginResponse>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(1, await check.Users.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, await check.RefreshTokens.CountAsync(TestContext.Current.CancellationToken));
    }

    [Theory(Timeout = 10_000)]
    [InlineData("not-an-email", TestAuth.StrongPassword)]
    [InlineData("a@test.local", "short1!")]
    [InlineData("a@test.local", "alllowercase1!")]
    [InlineData("a@test.local", "NoDigitsHere!")]
    [InlineData("a@test.local", "NoSpecial123")]
    public async Task Register_Invalid_Returns400_AndCreatesNothing(string email, string password)
    {
        var response = await RegisterAsync(new RegisterRequest(email, password, "Имя"));

        var result = ResultAssert.Unwrap<LoginResponse>(response);
        Assert.Equal(400, result.Code);
        Assert.NotEmpty(result.Errors!);

        await using var check = _testDb.NewContext();
        Assert.Equal(0, await check.Users.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await check.RefreshTokens.CountAsync(TestContext.Current.CancellationToken));
    }
}
