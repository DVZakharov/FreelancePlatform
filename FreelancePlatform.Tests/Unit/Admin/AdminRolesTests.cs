using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.Features.Admin.DemoteUser;
using FreelancePlatform.Application.Features.Admin.PromoteUser;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Tests.Unit.Admin;

public class AdminRolesTests
{
    private readonly TestDb _testDb = new();

    private async Task<Guid> SeedUserAsync(UserRole role)
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser(role: role);
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user.Id;
    }

    private Task<IResult> PromoteAsync(Guid id) =>
        _testDb.RunAsync(db => PromoteUserEndpoint.Handle(
            id.ToString(), db, TestContext.Current.CancellationToken));

    private Task<IResult> DemoteAsync(Guid id) =>
        _testDb.RunAsync(db => DemoteUserEndpoint.Handle(
            id.ToString(), db, TestContext.Current.CancellationToken));

    [Fact(Timeout = 10_000)]
    public async Task Promote_AddsAdminFlag_AndKeepsClient()
    {
        var id = await SeedUserAsync(UserRole.Client);

        var response = await PromoteAsync(id);

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);

        await using var check = _testDb.NewContext();
        var role = (await check.Users.SingleAsync(TestContext.Current.CancellationToken)).Role;
        Assert.True(role.HasFlag(UserRole.Admin) && role.HasFlag(UserRole.Client));
    }

    [Fact(Timeout = 10_000)]
    public async Task Promote_AlreadyAdmin_Returns409()
    {
        var id = await SeedUserAsync(UserRole.Admin);

        Assert.Equal(409, ResultAssert.Unwrap<object>(await PromoteAsync(id)).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Promote_UnknownUser_Returns404() =>
        Assert.Equal(404, ResultAssert.Unwrap<object>(await PromoteAsync(Guid.NewGuid())).Code);

    [Fact(Timeout = 10_000)]
    public async Task Demote_RemovesAdminFlag_KeepsOtherRoles_AndRevokesSessions()
    {
        var keep = await SeedUserAsync(UserRole.Admin);
        var target = await SeedUserAsync(UserRole.Admin | UserRole.Client);

        await using (var db = _testDb.NewContext())
        {
            db.RefreshTokens.AddRange(
                new RefreshTokenEntity { UserId = target, Token = "a", ExpiresAt = DateTime.UtcNow.AddDays(1) },
                new RefreshTokenEntity { UserId = target, Token = "b", ExpiresAt = DateTime.UtcNow.AddDays(1) },
                new RefreshTokenEntity { UserId = keep, Token = "c", ExpiresAt = DateTime.UtcNow.AddDays(1) });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await DemoteAsync(target);

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);

        await using var check = _testDb.NewContext();
        var role = (await check.Users.SingleAsync(u => u.Id == target, TestContext.Current.CancellationToken)).Role;
        Assert.False(role.HasFlag(UserRole.Admin));
        Assert.True(role.HasFlag(UserRole.Client));

        Assert.All(check.RefreshTokens.Where(t => t.UserId == target), t => Assert.NotNull(t.RevokedAt));
        Assert.Null((await check.RefreshTokens.SingleAsync(t => t.Token == "c", TestContext.Current.CancellationToken)).RevokedAt);
    }

    [Fact(Timeout = 10_000)]
    public async Task Demote_LastAdmin_Returns409_AndKeepsRole()
    {
        var only = await SeedUserAsync(UserRole.Admin);

        var response = await DemoteAsync(only);

        Assert.Equal(409, ResultAssert.Unwrap<object>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.True((await check.Users.SingleAsync(TestContext.Current.CancellationToken)).Role.HasFlag(UserRole.Admin));
    }

    [Fact(Timeout = 10_000)]
    public async Task Demote_NonAdmin_Returns409()
    {
        await SeedUserAsync(UserRole.Admin);
        var regular = await SeedUserAsync(UserRole.Client);

        Assert.Equal(409, ResultAssert.Unwrap<object>(await DemoteAsync(regular)).Code);
    }
}