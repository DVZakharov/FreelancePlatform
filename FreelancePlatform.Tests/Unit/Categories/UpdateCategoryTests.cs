using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Categories.UpdateCategory;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Categories;

public class UpdateCategoryTests
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

    private Task<Microsoft.AspNetCore.Http.IResult> UpdateAsync(string id, string name) =>
        _testDb.RunAsync(db => UpdateCategoryEndpoint.Handle(
            id, new UpdateCategoryRequest(name), new UpdateCategoryValidator(), db, TestContext.Current.CancellationToken));

    private async Task<List<string>> NamesAsync()
    {
        await using var check = _testDb.NewContext();
        var names = await check.Categories.Select(c => c.Name).ToListAsync(TestContext.Current.CancellationToken);
        return [.. names.Order(StringComparer.Ordinal)]; // не зависит от культуры (кириллица vs латиница)
    }

    [Fact]
    public async Task Update_SavesTrimmedName()
    {
        var id = await SeedAsync("Web");

        var response = await UpdateAsync(id.ToString(), "  Mobile  ");

        var result = ResultAssert.Unwrap<CategoryDto>(response);
        Assert.Equal(200, result.Code);
        Assert.Equal("Mobile", result.Value!.Name);
        Assert.Equal(new[] { "Mobile" }, await NamesAsync());
    }

    [Fact]
    public async Task Update_ChangingOnlyCaseOfOwnName_IsAllowed()
    {
        var id = await SeedAsync("дизайн");

        var response = await UpdateAsync(id.ToString(), "Дизайн");

        Assert.Equal(200, ResultAssert.Unwrap<CategoryDto>(response).Code);
        Assert.Equal(new[] { "Дизайн" }, await NamesAsync());
    }
    
    [Theory]
    [InlineData("Дизайн")]
    [InlineData("ДИЗАЙН")]
    [InlineData("  дизайн ")]
    public async Task Update_ToNameOfAnotherCategoryIgnoringCase_Returns409(string newName)
    {
        var id = await SeedAsync("Web");
        await SeedAsync("Дизайн");

        var response = await UpdateAsync(id.ToString(), newName);

        Assert.Equal(409, ResultAssert.Unwrap<CategoryDto>(response).Code);
        Assert.Equal(new[] { "Web", "Дизайн" }, await NamesAsync());
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var response = await UpdateAsync(Guid.NewGuid().ToString(), "Mobile");

        Assert.Equal(404, ResultAssert.Unwrap<CategoryDto>(response).Code);
    }

    [Fact]
    public async Task Update_InvalidId_Returns400()
    {
        var response = await UpdateAsync("not-a-guid", "Mobile");

        Assert.Equal(400, ResultAssert.Unwrap<CategoryDto>(response).Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    public async Task Update_InvalidName_Returns400_AndChangesNothing(string name)
    {
        var id = await SeedAsync("Web");

        var response = await UpdateAsync(id.ToString(), name);

        Assert.Equal(400, ResultAssert.Unwrap<CategoryDto>(response).Code);
        Assert.Equal(new[] { "Web" }, await NamesAsync());
    }
}