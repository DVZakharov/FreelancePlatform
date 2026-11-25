using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.FreelancerProfiles.CreateProfile;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Tests.Unit.FreelancerProfiles;

public class CreateProfileTests
{
    private readonly TestDb _testDb = new();
    private static readonly string Bio = new('a', 60);   // минимум по валидатору 50

    private async Task<(Guid UserId, List<Guid> SkillIds)> SeedAsync(int skills = 2)
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser(role: UserRole.Client);
        var list = Enumerable.Range(0, skills)
            .Select(i => new SkillEntity { Name = $"Skill {i} {Guid.NewGuid()}" }).ToList();
        db.Users.Add(user);
        db.Skills.AddRange(list);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (user.Id, [.. list.Select(s => s.Id)]);
    }

    private Task<IResult> CreateAsync(Guid userId, IEnumerable<string> skillIds, decimal rate = 25) =>
        _testDb.RunAsync(db => CreateProfileEndpoint.Handle(
            new CreateProfileRequest(Bio, rate, [.. skillIds]),
            TestHttp.As(userId), new CreateProfileValidator(), db, TestContext.Current.CancellationToken));

    [Fact(Timeout = 10_000)]
    public async Task Create_SavesProfileWithSkills_AndAddsFreelancerRole()
    {
        var (userId, skills) = await SeedAsync();

        var response = await CreateAsync(userId, skills.Select(s => s.ToString()));

        Assert.Equal(201, ResultAssert.Unwrap<FreelancerProfileDto>(response).Code);

        await using var check = _testDb.NewContext();
        var profile = await check.FreelancerProfiles.Include(p => p.Skills).SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, profile.Skills.Count);
        Assert.Equal(25, profile.HourlyRate);

        var role = (await check.Users.SingleAsync(TestContext.Current.CancellationToken)).Role;
        Assert.True(role.HasFlag(UserRole.Freelancer));
        Assert.True(role.HasFlag(UserRole.Client));   // старая роль не потеряна
    }

    [Fact(Timeout = 10_000)]
    public async Task Create_Twice_Returns409_AndKeepsSingleProfile()
    {
        var (userId, skills) = await SeedAsync();
        var ids = skills.Select(s => s.ToString()).ToList();

        await CreateAsync(userId, ids);
        var second = await CreateAsync(userId, ids);

        Assert.Equal(409, ResultAssert.Unwrap<FreelancerProfileDto>(second).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(1, await check.FreelancerProfiles.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact(Timeout = 10_000)]
    public async Task Create_WithUnknownSkill_Returns400_AndLeavesRoleUntouched()
    {
        var (userId, skills) = await SeedAsync();

        var response = await CreateAsync(userId, [skills[0].ToString(), Guid.NewGuid().ToString()]);

        Assert.Equal(400, ResultAssert.Unwrap<FreelancerProfileDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Empty(check.FreelancerProfiles);
        Assert.False((await check.Users.SingleAsync(TestContext.Current.CancellationToken)).Role.HasFlag(UserRole.Freelancer));
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task Create_WithMalformedSkillId_Returns400(string badId)
    {
        var (userId, _) = await SeedAsync();

        var response = await CreateAsync(userId, [badId]);

        Assert.Equal(400, ResultAssert.Unwrap<FreelancerProfileDto>(response).Code);
    }

    [Fact]
    public async Task Create_WithNoSkills_Returns400()
    {
        var (userId, _) = await SeedAsync();

        Assert.Equal(400, ResultAssert.Unwrap<FreelancerProfileDto>(await CreateAsync(userId, [])).Code);
    }
}