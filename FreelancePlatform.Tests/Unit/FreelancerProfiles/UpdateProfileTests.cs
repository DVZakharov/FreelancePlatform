using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.FreelancerProfiles.UpdateProfile;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.FreelancerProfiles;

public class UpdateProfileTests
{
    private readonly TestDb _testDb = new();
    private static readonly string NewBio = new('b', 60);

    private async Task<(Guid UserId, Guid OldSkill, Guid NewSkill)> SeedProfileAsync(decimal rating)
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser();
        var oldSkill = new SkillEntity { Name = $"Old {Guid.NewGuid()}" };
        var newSkill = new SkillEntity { Name = $"New {Guid.NewGuid()}" };
        var profile = new FreelancerProfileEntity
        {
            UserId = user.Id, Bio = "old bio", HourlyRate = 10, Rating = rating, Skills = [oldSkill]
        };
        db.AddRange(user, newSkill, profile);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (user.Id, oldSkill.Id, newSkill.Id);
    }

    [Fact(Timeout = 10_000)]
    public async Task Update_ReplacesFields_AndSkills_ButNeverTouchesRating()
    {
        var (userId, oldSkill, newSkill) = await SeedProfileAsync(rating: 4.5m);

        var response = await _testDb.RunAsync(db => UpdateProfileEndpoint.Handle(
            new UpdateProfileRequest(NewBio, 99, [newSkill.ToString()]),
            TestHttp.As(userId), new UpdateProfileValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(200, ResultAssert.Unwrap<FreelancerProfileDto>(response).Code);

        await using var check = _testDb.NewContext();
        var profile = await check.FreelancerProfiles.Include(p => p.Skills).SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(NewBio, profile.Bio);
        Assert.Equal(99, profile.HourlyRate);
        Assert.Equal(newSkill, Assert.Single(profile.Skills).Id);
        Assert.DoesNotContain(profile.Skills, s => s.Id == oldSkill);
        Assert.Equal(4.5m, profile.Rating);
    }

    [Fact(Timeout = 10_000)]
    public async Task Update_WithoutProfile_Returns404()
    {
        var (_, _, newSkill) = await SeedProfileAsync(0);

        var response = await _testDb.RunAsync(db => UpdateProfileEndpoint.Handle(
            new UpdateProfileRequest(NewBio, 99, [newSkill.ToString()]),
            TestHttp.As(Guid.NewGuid()), new UpdateProfileValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(404, ResultAssert.Unwrap<FreelancerProfileDto>(response).Code);
    }
}