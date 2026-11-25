using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Skills.CreateSkill;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Skills;

public class CreateSkillTests
{
    private readonly TestDb _testDb = new();

    private Task<IResult> CreateAsync(string name) =>
        _testDb.RunAsync(db => CreateSkillEndpoint.Handle(
            new CreateSkillRequest(name), new CreateSkillValidator(), db, TestContext.Current.CancellationToken));

    private async Task SeedAsync(string name)
    {
        await using var db = _testDb.NewContext();
        db.Skills.Add(new SkillEntity { Name = name });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Timeout = 10_000)]
    public async Task Create_SavesTrimmedName()
    {
        var response = await CreateAsync("  C#  ");

        var result = ResultAssert.Unwrap<SkillDto>(response);
        Assert.Equal(201, result.Code);
        Assert.Equal("C#", result.Value!.Name);

        await using var check = _testDb.NewContext();
        Assert.Equal("C#", (await check.Skills.SingleAsync(TestContext.Current.CancellationToken)).Name);
    }

    [Theory(Timeout = 10_000)]
    [InlineData("C#", "c#")]
    [InlineData("C#", "  C#  ")]
    [InlineData("Дизайн", "ДИЗАЙН")]
    public async Task Create_DuplicateIgnoringCaseAndSpaces_Returns409(string existing, string duplicate)
    {
        await SeedAsync(existing);

        var response = await CreateAsync(duplicate);

        Assert.Equal(409, ResultAssert.Unwrap<SkillDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(1, await check.Skills.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact(Timeout = 10_000)]
    public async Task Create_SimilarButDifferentName_Succeeds()
    {
        await SeedAsync("C#");

        var response = await CreateAsync("C++");

        Assert.Equal(201, ResultAssert.Unwrap<SkillDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(2, await check.Skills.CountAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_EmptyName_Returns400_AndSavesNothing(string name)
    {
        var response = await CreateAsync(name);

        Assert.Equal(400, ResultAssert.Unwrap<SkillDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Empty(check.Skills);
    }
}