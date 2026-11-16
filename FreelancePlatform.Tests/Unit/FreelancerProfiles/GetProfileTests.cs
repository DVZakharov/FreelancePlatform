using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.FreelancerProfiles.GetMyProfile;
using FreelancePlatform.Application.Features.FreelancerProfiles.GetProfileById;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.FreelancerProfiles;

public class GetProfileTests
{
    private readonly TestDb _testDb = new();

    private async Task<(Guid UserId, Guid ProfileId)> SeedProfileAsync()
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser();
        user.FullName = "Анна Петрова";
        var profile = new FreelancerProfileEntity
        {
            UserId = user.Id, Bio = "bio", HourlyRate = 30, Rating = 4.5m,
            Skills = [new SkillEntity { Name = $"C# {Guid.NewGuid()}" }]
        };
        db.AddRange(user, profile);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (user.Id, profile.Id);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetById_ReturnsProfileWithUserNameAndSkills()
    {
        var (userId, profileId) = await SeedProfileAsync();

        var response = await _testDb.RunAsync(db => GetProfileByIdEndpoint.Handle(
            profileId.ToString(), new GetProfileByIdValidator(), db, TestContext.Current.CancellationToken));

        var result = ResultAssert.Unwrap<FreelancerProfileDto>(response);
        Assert.Equal(200, result.Code);
        Assert.Equal(userId, result.Value!.UserId);
        Assert.Equal("Анна Петрова", result.Value.FullName);
        Assert.Single(result.Value.Skills);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetById_UnknownId_Returns404()
    {
        var response = await _testDb.RunAsync(db => GetProfileByIdEndpoint.Handle(
            Guid.NewGuid().ToString(), new GetProfileByIdValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(404, ResultAssert.Unwrap<FreelancerProfileDto>(response).Code);
    }

    [Theory(Timeout = 10_000)]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task GetById_InvalidId_Returns400(string id)
    {
        var response = await _testDb.RunAsync(db => GetProfileByIdEndpoint.Handle(
            id, new GetProfileByIdValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<FreelancerProfileDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetMy_ReturnsOwnProfile()
    {
        var (userId, profileId) = await SeedProfileAsync();

        var response = await _testDb.RunAsync(db => GetMyProfileEndpoint.Handle(TestHttp.As(userId), db, TestContext.Current.CancellationToken));

        var result = ResultAssert.Unwrap<FreelancerProfileDto>(response);
        Assert.Equal(200, result.Code);
        Assert.Equal(profileId, result.Value!.Id);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetMy_WithoutProfile_Returns404()
    {
        await using (var db = _testDb.NewContext())
        {
            db.Users.Add(Seed.NewUser());
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _testDb.RunAsync(db =>
            GetMyProfileEndpoint.Handle(TestHttp.As(db.Users.Single().Id), db, TestContext.Current.CancellationToken));

        Assert.Equal(404, ResultAssert.Unwrap<FreelancerProfileDto>(response).Code);
    }
}