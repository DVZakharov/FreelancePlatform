using FreelancePlatform.Application.Features.Skills.DeleteSkill;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Skills;

public class DeleteSkillTests
{
    private readonly TestDb _testDb = new();

    private async Task<List<string>> NamesAsync()
    {
        await using var check = _testDb.NewContext();
        var names = await check.Skills.Select(s => s.Name).ToListAsync(TestContext.Current.CancellationToken);
        return [.. names.Order(StringComparer.Ordinal)]; // не зависит от культуры (кириллица vs латиница)
    }
    
    private async Task<Guid> SeedAsync(string name)
    {
        await using var db = _testDb.NewContext();
        var skill = new SkillEntity { Name = name };
        db.Skills.Add(skill);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return skill.Id;
    }
    
    private Task<Microsoft.AspNetCore.Http.IResult> DeleteAsync(string id, CancellationToken ct) =>
        _testDb.RunAsync(db => DeleteSkillEndpoint.Handle(id, new DeleteSkillValidator(), db, ct));
    
    [Fact(Timeout = 10_000)]
    public async Task Delete_RemovesOnlyTheTargetSkill()
    {
        var id = await SeedAsync("C#");
        await SeedAsync("Rust");

        var response = await DeleteAsync(id.ToString(), TestContext.Current.CancellationToken);

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);
        Assert.Equal(new[] { "Rust" }, await NamesAsync());
    }

    [Fact(Timeout = 10_000)]
    public async Task Delete_UnknownId_Returns404()
    {
        var response = await DeleteAsync(Guid.NewGuid().ToString(), TestContext.Current.CancellationToken);

        Assert.Equal(404, ResultAssert.Unwrap<object>(response).Code);
    }

    [Theory(Timeout = 10_000)]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task Delete_InvalidId_Returns400(string id)
    {
        var response = await DeleteAsync(id, TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<object>(response).Code);
    }
}