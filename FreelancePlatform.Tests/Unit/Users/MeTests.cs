using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Users.GetMe;
using FreelancePlatform.Application.Features.Users.UpdateMe;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Users;

public class MeTests
{
    private readonly TestDb _testDb = new();

    private async Task<UserEntity> SeedUserAsync(UserRole role = UserRole.Client, bool withProfile = false)
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser(balance: 250, role: role);
        user.FullName = "Анна Петрова";
        db.Users.Add(user);
        if (withProfile)
            db.FreelancerProfiles.Add(new FreelancerProfileEntity { UserId = user.Id, Bio = "bio", HourlyRate = 10 });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user;
    }

    [Fact(Timeout = 10_000)]
    public async Task GetMe_ReturnsRolesBalanceAndFreelancerFlag()
    {
        var user = await SeedUserAsync(UserRole.Client | UserRole.Freelancer, withProfile: true);

        var response = await _testDb.RunAsync(db => GetMeEndpoint.Handle(TestHttp.As(user.Id), db, TestContext.Current.CancellationToken));

        var result = ResultAssert.Unwrap<UserDto>(response);
        Assert.Equal(200, result.Code);
        Assert.Equal(user.Email, result.Value!.Email);
        Assert.Equal(250m, result.Value.Balance);
        Assert.True(result.Value.IsFreelancer);
        Assert.Equal(new[] { "Client", "Freelancer" }, result.Value.Roles.OrderBy(r => r).ToArray());
    }

    [Fact(Timeout = 10_000)]
    public async Task GetMe_WithoutProfile_IsNotFreelancer()
    {
        var user = await SeedUserAsync();

        var response = await _testDb.RunAsync(db => GetMeEndpoint.Handle(TestHttp.As(user.Id), db, TestContext.Current.CancellationToken));

        Assert.False(ResultAssert.Unwrap<UserDto>(response).Value!.IsFreelancer);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetMe_DeletedUser_Returns404()
    {
        var response = await _testDb.RunAsync(db => GetMeEndpoint.Handle(TestHttp.As(Guid.NewGuid()), db, TestContext.Current.CancellationToken));

        Assert.Equal(404, ResultAssert.Unwrap<UserDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task UpdateMe_ChangesNameOnly()
    {
        var user = await SeedUserAsync();

        var response = await _testDb.RunAsync(db => UpdateMeEndpoint.Handle(
            new UpdateMeRequest("Новое Имя"), TestHttp.As(user.Id), new UpdateMeValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(200, ResultAssert.Unwrap<UserDto>(response).Code);
        await using var check = _testDb.NewContext();
        var saved = await check.Users.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Новое Имя", saved.FullName);
        Assert.Equal(user.Email, saved.Email);
        Assert.Equal(250m, saved.Balance);
        Assert.Equal(UserRole.Client, saved.Role);
    }

    [Theory(Timeout = 10_000)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateMe_BlankName_Returns400_AndChangesNothing(string name)
    {
        var user = await SeedUserAsync();

        var response = await _testDb.RunAsync(db => UpdateMeEndpoint.Handle(
            new UpdateMeRequest(name), TestHttp.As(user.Id), new UpdateMeValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<UserDto>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal("Анна Петрова", (await check.Users.SingleAsync(TestContext.Current.CancellationToken)).FullName);
    }
}