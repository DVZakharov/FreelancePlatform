using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Projects.UpdateProject;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Projects;

public class UpdateProjectTests
{
    private readonly TestDb _testDb = new();

    private async Task<(Guid CategoryId, List<Guid> SkillIds)> SeedCategoryAndSkillsAsync(int skills = 2)
    {
        await using var db = _testDb.NewContext();
        var category = new CategoryEntity { Name = $"Category {Guid.NewGuid()}" };
        var skillEntities = Enumerable.Range(0, skills)
            .Select(_ => new SkillEntity { Name = $"Skill {Guid.NewGuid()}" }).ToList();
        db.Add(category);
        db.AddRange(skillEntities);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (category.Id, [.. skillEntities.Select(s => s.Id)]);
    }

    private static UpdateProjectRequest Request(
        Guid categoryId, IEnumerable<Guid> skillIds,
        string title = "Новый заголовок", string budgetType = "Hourly",
        decimal min = 10, decimal max = 20,
        int descriptionLength = 60, int deadlineInDays = 30) =>
        new(categoryId.ToString(), title, new string('a', descriptionLength), budgetType, min, max,
            DateTime.UtcNow.AddDays(deadlineInDays), [.. skillIds.Select(s => s.ToString())]);

    private Task<Microsoft.AspNetCore.Http.IResult> UpdateAsync(string id, UpdateProjectRequest request, Guid userId) =>
        _testDb.RunAsync(db => UpdateProjectEndpoint.Handle(
            id, request, TestHttp.As(userId), new UpdateProjectValidator(), db, TestContext.Current.CancellationToken));

    [Theory(Timeout = 10_000)]
    [InlineData(ProjectStatus.Draft)]
    [InlineData(ProjectStatus.Open)]
    public async Task Update_DraftOrOpen_ByOwner_ReplacesFieldsAndSkills(ProjectStatus status)
    {
        var s = await Seed.DraftProjectAsync(_testDb, status: status);
        var (categoryId, skillIds) = await SeedCategoryAndSkillsAsync();

        var response = await UpdateAsync(s.ProjectId.ToString(), Request(categoryId, skillIds), s.ClientId);

        Assert.Equal(200, ResultAssert.Unwrap<ProjectDto>(response).Code);

        await using var check = _testDb.NewContext();
        var project = await check.Projects.Include(p => p.RequiredSkills).SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("Новый заголовок", project.Title);
        Assert.Equal(BudgetType.Hourly, project.BudgetType);
        Assert.Equal(categoryId, project.CategoryId);
        Assert.Equal(skillIds.Order(), project.RequiredSkills.Select(x => x.Id).Order());
        Assert.Equal(status, project.Status);
    }

    [Theory(Timeout = 10_000)]
    [InlineData(ProjectStatus.InProgress)]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Cancelled)]
    public async Task Update_AfterWorkStarted_Returns409_AndChangesNothing(ProjectStatus status)
    {
        var s = await Seed.DraftProjectAsync(_testDb, title: "Старый", status: status);
        var (categoryId, skillIds) = await SeedCategoryAndSkillsAsync();

        var response = await UpdateAsync(s.ProjectId.ToString(), Request(categoryId, skillIds), s.ClientId);

        Assert.Equal(409, ResultAssert.Unwrap<ProjectDto>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal("Старый", (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Title);
    }

    [Fact(Timeout = 10_000)]
    public async Task Update_ByNonOwner_Returns403_AndChangesNothing()
    {
        var s = await Seed.DraftProjectAsync(_testDb, title: "Старый");
        var (categoryId, skillIds) = await SeedCategoryAndSkillsAsync();

        var response = await UpdateAsync(s.ProjectId.ToString(), Request(categoryId, skillIds), Guid.NewGuid());

        Assert.Equal(403, ResultAssert.Unwrap<ProjectDto>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal("Старый", (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Title);
    }

    [Fact]
    public async Task Update_UnknownProject_Returns404()
    {
        var (categoryId, skillIds) = await SeedCategoryAndSkillsAsync();

        var response = await UpdateAsync(Guid.NewGuid().ToString(), Request(categoryId, skillIds), Guid.NewGuid());

        Assert.Equal(404, ResultAssert.Unwrap<ProjectDto>(response).Code);
    }

    [Fact]
    public async Task Update_InvalidProjectId_Returns400()
    {
        var (categoryId, skillIds) = await SeedCategoryAndSkillsAsync();

        var response = await UpdateAsync("not-a-guid", Request(categoryId, skillIds), Guid.NewGuid());

        Assert.Equal(400, ResultAssert.Unwrap<ProjectDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Update_UnknownCategory_Returns404_AndChangesNothing()
    {
        var s = await Seed.DraftProjectAsync(_testDb, title: "Старый");
        var (_, skillIds) = await SeedCategoryAndSkillsAsync();

        var response = await UpdateAsync(s.ProjectId.ToString(), Request(Guid.NewGuid(), skillIds), s.ClientId);

        Assert.Equal(404, ResultAssert.Unwrap<ProjectDto>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal("Старый", (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Title);
    }

    [Fact(Timeout = 10_000)]
    public async Task Update_UnknownSkill_Returns400_AndKeepsOldSkills()
    {
        var s = await Seed.DraftProjectAsync(_testDb, skillsCount: 1);
        var (categoryId, skillIds) = await SeedCategoryAndSkillsAsync(1);

        var response = await UpdateAsync(
            s.ProjectId.ToString(), Request(categoryId, [.. skillIds, Guid.NewGuid()]), s.ClientId);

        Assert.Equal(400, ResultAssert.Unwrap<ProjectDto>(response).Code);
        await using var check = _testDb.NewContext();
        var project = await check.Projects.Include(p => p.RequiredSkills).SingleAsync(TestContext.Current.CancellationToken);
        Assert.Single(project.RequiredSkills);
        Assert.DoesNotContain(project.RequiredSkills, x => skillIds.Contains(x.Id));
    }

    // Параметры: описание, min, max, дедлайн (дни), навыки (0 = пустой список)
    [Theory(Timeout = 10_000)]
    [InlineData(10, 10, 20, 30, 0)]   // нет навыков
    [InlineData(49, 10, 20, 30, 1)]   // описание короче 50
    [InlineData(60, 0, 20, 30, 1)]    // нулевой бюджет
    [InlineData(60, 10, 20, -1, 1)]   // дедлайн в прошлом
    public async Task Update_OpenProject_WithDataPublishWouldReject_Returns400_AndChangesNothing(
        int descriptionLength, decimal min, decimal max, int deadlineInDays, int skills)
    {
        var s = await Seed.DraftProjectAsync(_testDb, title: "Старый", status: ProjectStatus.Open);
        var (categoryId, skillIds) = await SeedCategoryAndSkillsAsync(Math.Max(skills, 1));
        var request = Request(categoryId, skillIds.Take(skills), min: min, max: max,
            descriptionLength: descriptionLength, deadlineInDays: deadlineInDays);

        var response = await UpdateAsync(s.ProjectId.ToString(), request, s.ClientId);

        var result = ResultAssert.Unwrap<ProjectDto>(response);
        Assert.Equal(400, result.Code);
        Assert.NotEmpty(result.Errors!);
        await using var check = _testDb.NewContext();
        Assert.Equal("Старый", (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Title);
    }

    [Fact]
    public async Task Update_DraftProject_AllowsIncompleteData()
    {
        var s = await Seed.DraftProjectAsync(_testDb);
        var (categoryId, _) = await SeedCategoryAndSkillsAsync();
        var request = Request(categoryId, [], min: 0, max: 0, descriptionLength: 5, deadlineInDays: -1);

        var response = await UpdateAsync(s.ProjectId.ToString(), request, s.ClientId);

        Assert.Equal(200, ResultAssert.Unwrap<ProjectDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Update_NullSkillIds_Returns400_Not500()
    {
        var s = await Seed.DraftProjectAsync(_testDb, title: "Старый");
        var (categoryId, _) = await SeedCategoryAndSkillsAsync();
        var request = Request(categoryId, []) with { SkillIds = null! };

        var response = await UpdateAsync(s.ProjectId.ToString(), request, s.ClientId);

        Assert.Equal(400, ResultAssert.Unwrap<ProjectDto>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal("Старый", (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Title);
    }

    [Fact(Timeout = 10_000)]
    public async Task Update_InvalidBody_Returns400_AndChangesNothing()
    {
        var s = await Seed.DraftProjectAsync(_testDb, title: "Старый");
        var (categoryId, skillIds) = await SeedCategoryAndSkillsAsync();

        foreach (var bad in new[]
                 {
                     Request(categoryId, skillIds, title: ""),
                     Request(categoryId, skillIds, budgetType: "Weekly"),
                     Request(categoryId, skillIds, min: 100, max: 50),
                     Request(categoryId, skillIds, min: -1, max: 50),
                 })
        {
            var response = await UpdateAsync(s.ProjectId.ToString(), bad, s.ClientId);
            Assert.Equal(400, ResultAssert.Unwrap<ProjectDto>(response).Code);
        }

        await using var check = _testDb.NewContext();
        Assert.Equal("Старый", (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Title);
    }
}
