using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Skills.UpdateSkill;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Skills;

public class UpdateSkillTests
{
    private readonly TestDb _testDb = new();

    private async Task<Guid> SeedAsync(string name)
    {
        await using var db = _testDb.NewContext();
        var skill = new SkillEntity { Name = name };
        db.Skills.Add(skill);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return skill.Id;
    }

    private Task<Microsoft.AspNetCore.Http.IResult> UpdateAsync(string id, string name, CancellationToken ct) =>
        _testDb.RunAsync(db => UpdateSkillEndpoint.Handle(
            id, new UpdateSkillRequest(name), new UpdateSkillValidator(), db, ct));

    private async Task<List<string>> NamesAsync()
    {
        await using var check = _testDb.NewContext();
        var names = await check.Skills.Select(s => s.Name).ToListAsync(TestContext.Current.CancellationToken);
        return [.. names.Order(StringComparer.Ordinal)]; // не зависит от культуры (кириллица vs латиница)
    }

    [Fact(Timeout = 10_000)]
    public async Task Update_SavesTrimmedName()
    {
        var id = await SeedAsync("C#");

        var response = await UpdateAsync(id.ToString(), "  Rust  ", TestContext.Current.CancellationToken);

        var result = ResultAssert.Unwrap<SkillDto>(response);
        Assert.Equal(200, result.Code);
        Assert.Equal("Rust", result.Value!.Name);
        Assert.Equal(new[] { "Rust" }, await NamesAsync());
    }

    [Fact(Timeout = 10_000)]
    public async Task Update_ChangingOnlyCaseOfOwnName_IsAllowed()
    {
        var id = await SeedAsync("c#");

        var response = await UpdateAsync(id.ToString(), "C#", TestContext.Current.CancellationToken);

        Assert.Equal(200, ResultAssert.Unwrap<SkillDto>(response).Code);
        Assert.Equal(new[] { "C#" }, await NamesAsync());
    }

    [Theory(Timeout = 10_000)]
    [InlineData("Rust")]
    [InlineData("RUST")]
    [InlineData("  rust ")]
    public async Task Update_ToNameOfAnotherSkillIgnoringCase_Returns409(string newName)
    {
        var id = await SeedAsync("C#");
        await SeedAsync("Rust");

        var response = await UpdateAsync(id.ToString(), newName, TestContext.Current.CancellationToken);

        Assert.Equal(409, ResultAssert.Unwrap<SkillDto>(response).Code);
        Assert.Equal(new[] { "C#", "Rust" }, await NamesAsync());
    }

    [Fact(Timeout = 10_000)]
    public async Task Update_UnknownId_Returns404()
    {
        var response = await UpdateAsync(Guid.NewGuid().ToString(), "Rust", TestContext.Current.CancellationToken);

        Assert.Equal(404, ResultAssert.Unwrap<SkillDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Update_InvalidId_Returns400()
    {
        var response = await UpdateAsync("not-a-guid", "Rust", TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<SkillDto>(response).Code);
    }

    [Theory(Timeout = 10_000)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    public async Task Update_InvalidName_Returns400_AndChangesNothing(string name)
    {
        var id = await SeedAsync("C#");

        var response = await UpdateAsync(id.ToString(), name, TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<SkillDto>(response).Code);
        Assert.Equal(new[] { "C#" }, await NamesAsync());
    }
}