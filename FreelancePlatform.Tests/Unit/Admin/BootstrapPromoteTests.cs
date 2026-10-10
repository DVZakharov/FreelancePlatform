using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FreelancePlatform.Application.Features.Admin.BootstrapPromote;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Tests.Unit.Admin;

public class BootstrapPromoteTests
{
    private const string Secret = "right-secret";
    private readonly TestDb _testDb = new();

    private static IConfiguration Config(string? secret) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(secret is null
                ? new Dictionary<string, string?>()
                : new Dictionary<string, string?> { ["Admin:SetupSecret"] = secret })
            .Build();

    private async Task<UserEntity> SeedUserAsync(UserRole role = UserRole.Client)
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser(role: role);
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    private Task<IResult> BootstrapAsync(string email, string secret, string? configuredSecret = Secret) =>
        _testDb.RunAsync(db => BootstrapPromoteEndpoint.Handle(
            new BootstrapPromoteRequest(email, secret),
            new BootstrapPromoteValidator(), db, Config(configuredSecret), TestContext.Current.CancellationToken));

    [Fact(Timeout = 10_000)]
    public async Task Bootstrap_WithRightSecret_MakesUserAdmin_AndKeepsOtherRoles()
    {
        var user = await SeedUserAsync();

        var response = await BootstrapAsync(user.Email, Secret);

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);

        await using var check = _testDb.NewContext();
        var role = (await check.Users.SingleAsync(TestContext.Current.CancellationToken)).Role;
        Assert.True(role.HasFlag(UserRole.Admin));
        Assert.True(role.HasFlag(UserRole.Client));
    }

    [Fact(Timeout = 10_000)]
    public async Task Bootstrap_WithWrongSecret_Returns403_AndPromotesNobody()
    {
        var user = await SeedUserAsync();

        var response = await BootstrapAsync(user.Email, "wrong");

        Assert.Equal(403, ResultAssert.Unwrap<object>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.False((await check.Users.SingleAsync(TestContext.Current.CancellationToken)).Role.HasFlag(UserRole.Admin));
    }

    [Fact(Timeout = 10_000)]
    public async Task Bootstrap_WhenSecretNotConfigured_Returns403_EvenIfRequestSecretIsEmptyish()
    {
        var user = await SeedUserAsync();

        // Секрет в конфиге не задан. Защита от случая "забыли настроить" и сравнения null == null
        var response = await BootstrapAsync(user.Email, "anything", configuredSecret: null);

        Assert.Equal(403, ResultAssert.Unwrap<object>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Bootstrap_WhenAdminAlreadyExists_Returns403_EvenWithRightSecret()
    {
        await SeedUserAsync(UserRole.Admin);
        var other = await SeedUserAsync();

        var response = await BootstrapAsync(other.Email, Secret);

        Assert.Equal(403, ResultAssert.Unwrap<object>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(1, await check.Users.CountAsync(u => (u.Role & UserRole.Admin) == UserRole.Admin, TestContext.Current.CancellationToken));
    }

    [Fact(Timeout = 10_000)]
    public async Task Bootstrap_UnknownEmail_Returns404()
    {
        var response = await BootstrapAsync("nobody@test.local", Secret);

        Assert.Equal(404, ResultAssert.Unwrap<object>(response).Code);
    }
}