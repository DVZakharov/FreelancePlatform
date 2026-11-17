using FreelancePlatform.Application.Features.Categories.DeleteCategory;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Categories;

public class DeleteCategoryTests
{
    private readonly TestDb _testDb = new();
    
    private async Task<Guid> SeedAsync(string name)
    {
        await using var db = _testDb.NewContext();
        var category = new CategoryEntity { Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return category.Id;
    }
    
    private async Task<List<string>> NamesAsync()
    {
        await using var check = _testDb.NewContext();
        return await check.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync(TestContext.Current.CancellationToken);
    }
    
    private Task<Microsoft.AspNetCore.Http.IResult> DeleteAsync(string id, CancellationToken ct) =>
        _testDb.RunAsync(db => DeleteCategoryEndpoint.Handle(id, new DeleteCategoryValidator(), db, ct));

    [Fact(Timeout = 10_000)]
    public async Task Delete_UnusedCategory_RemovesOnlyIt()
    {
        var id = await SeedAsync("Web");
        await SeedAsync("Mobile");

        var response = await DeleteAsync(id.ToString(), TestContext.Current.CancellationToken);

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);
        Assert.Equal(new[] { "Mobile" }, await NamesAsync());
    }
    
    [Fact(Timeout = 10_000)]
    public async Task Delete_CategoryWithProjects_Returns409_AndKeepsIt()
    {
        await Seed.DraftProjectAsync(_testDb);
        Guid categoryId;
        await using (var db = _testDb.NewContext())
            categoryId = (await db.Projects.SingleAsync(TestContext.Current.CancellationToken)).CategoryId;

        var response = await DeleteAsync(categoryId.ToString(), TestContext.Current.CancellationToken);

        Assert.Equal(409, ResultAssert.Unwrap<object>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal(1, await check.Categories.CountAsync(TestContext.Current.CancellationToken));
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