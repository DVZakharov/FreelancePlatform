using FreelancePlatform.Application.Features.Projects.SearchProjects;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Projects;

public class SearchProjectsTests
{
    private readonly TestDb _testDb = new();

    private static ProjectEntity NewProject(
        UserEntity client, CategoryEntity category, string title,
        ProjectStatus status = ProjectStatus.Open,
        decimal min = 100, decimal max = 500,
        DateTime? publishedAt = null, params SkillEntity[] skills)
    {
        var project = new ProjectEntity
        {
            ClientId = client.Id, CategoryId = category.Id, Title = title, Description = "Описание",
            BudgetType = BudgetType.Fixed, BudgetMin = min, BudgetMax = max,
            Deadline = DateTime.UtcNow.AddDays(10), Status = status,
            PublishedAt = publishedAt ?? DateTime.UtcNow
        };
        foreach (var skill in skills) project.RequiredSkills.Add(skill);
        return project;
    }

    private async Task<List<string>> TitlesAsync(SearchProjectsRequest request)
    {
        var response = await _testDb.RunAsync(db => SearchProjectsEndpoint.Handle(
            request, new SearchProjectsValidator(), db, TestContext.Current.CancellationToken));
        var result = ResultAssert.Unwrap<SearchProjectsResponse>(response);
        Assert.Equal(200, result.Code);
        return [.. result.Value!.Items.Select(i => i.Title)];
    }

    [Fact(Timeout = 10_000)]
    public async Task Search_ReturnsOnlyOpenProjects()
    {
        await using (var db = _testDb.NewContext())
        {
            var client = Seed.NewUser();
            var category = new CategoryEntity { Name = "Cat" };
            db.AddRange(client, category,
                NewProject(client, category, "open"),
                NewProject(client, category, "draft", ProjectStatus.Draft),
                NewProject(client, category, "inprogress", ProjectStatus.InProgress),
                NewProject(client, category, "completed", ProjectStatus.Completed),
                NewProject(client, category, "cancelled", ProjectStatus.Cancelled));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(new[] { "open" }, await TitlesAsync(new SearchProjectsRequest()));
    }

    [Fact(Timeout = 10_000)]
    public async Task Search_FiltersByCategory()
    {
        Guid webId;
        await using (var db = _testDb.NewContext())
        {
            var client = Seed.NewUser();
            var web = new CategoryEntity { Name = "Web" };
            var mobile = new CategoryEntity { Name = "Mobile" };
            webId = web.Id;
            db.AddRange(client, web, mobile,
                NewProject(client, web, "web-1"),
                NewProject(client, mobile, "mobile-1"));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(new[] { "web-1" }, await TitlesAsync(new SearchProjectsRequest { CategoryId = webId.ToString() }));
    }

    [Fact(Timeout = 10_000)]
    public async Task Search_SkillsFilter_RequiresAllSkills()
    {
        Guid a, b;
        await using (var db = _testDb.NewContext())
        {
            var client = Seed.NewUser();
            var category = new CategoryEntity { Name = "Cat" };
            var skillA = new SkillEntity { Name = "A" };
            var skillB = new SkillEntity { Name = "B" };
            (a, b) = (skillA.Id, skillB.Id);
            db.AddRange(client, category, skillA, skillB,
                NewProject(client, category, "both", skills: [skillA, skillB]),
                NewProject(client, category, "only-a", skills: [skillA]),
                NewProject(client, category, "none"));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(new[] { "both" }, await TitlesAsync(new SearchProjectsRequest { SkillIds = [a.ToString(), b.ToString()] }));
        Assert.Equal(["both", "only-a"],
            (await TitlesAsync(new SearchProjectsRequest { SkillIds = [a.ToString()] })).Order());
    }

    [Fact(Timeout = 10_000)]
    public async Task Search_BudgetFilter_MatchesOverlappingRanges()
    {
        await using (var db = _testDb.NewContext())
        {
            var client = Seed.NewUser();
            var category = new CategoryEntity { Name = "Cat" };
            db.AddRange(client, category,
                NewProject(client, category, "cheap", min: 100, max: 200),
                NewProject(client, category, "expensive", min: 500, max: 800));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(new[] { "expensive" }, await TitlesAsync(new SearchProjectsRequest { BudgetMin = 300 }));
        Assert.Equal(new[] { "cheap" }, await TitlesAsync(new SearchProjectsRequest { BudgetMax = 150 }));
        Assert.Equal(["cheap", "expensive"],
            (await TitlesAsync(new SearchProjectsRequest { BudgetMin = 150, BudgetMax = 600 })).Order());
        Assert.Empty(await TitlesAsync(new SearchProjectsRequest { BudgetMin = 300, BudgetMax = 400 }));
    }

    [Fact(Timeout = 10_000)]
    public async Task Search_Paginates_NewestFirst_AndReportsTotals()
    {
        await using (var db = _testDb.NewContext())
        {
            var client = Seed.NewUser();
            var category = new CategoryEntity { Name = "Cat" };
            db.AddRange(client, category);
            for (var i = 1; i <= 5; i++)
                db.Add(NewProject(client, category, $"p{i}", publishedAt: DateTime.UtcNow.AddHours(-10 + i)));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _testDb.RunAsync(db => SearchProjectsEndpoint.Handle(
            new SearchProjectsRequest { PageNumber = 2, PageSize = 2 }, new SearchProjectsValidator(), db, TestContext.Current.CancellationToken));

        var result = ResultAssert.Unwrap<SearchProjectsResponse>(response).Value!;
        Assert.Equal(new[] { "p3", "p2" }, result.Items.Select(i => i.Title).ToArray());
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.ReturnedCount);
        Assert.Equal(2, result.RequestedPageNumber);
    }

    [Fact(Timeout = 10_000)]
    public async Task Search_NothingFound_Returns200WithEmptyList()
    {
        var response = await _testDb.RunAsync(db => SearchProjectsEndpoint.Handle(
            new SearchProjectsRequest(), new SearchProjectsValidator(), db, TestContext.Current.CancellationToken));

        var result = ResultAssert.Unwrap<SearchProjectsResponse>(response);
        Assert.Equal(200, result.Code);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Theory(Timeout = 10_000)]
    [InlineData(0, 10, null, null)]
    [InlineData(1, 0, null, null)]
    [InlineData(1, 10, "not-a-guid", null)]
    [InlineData(1, 10, null, "not-a-guid")]
    public async Task Search_InvalidRequest_Returns400(int page, int size, string? categoryId, string? skillId)
    {
        var request = new SearchProjectsRequest
        {
            PageNumber = page, PageSize = size, CategoryId = categoryId,
            SkillIds = skillId is null ? null : [skillId]
        };

        var response = await _testDb.RunAsync(db => SearchProjectsEndpoint.Handle(
            request, new SearchProjectsValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<SearchProjectsResponse>(response).Code);
    }
}