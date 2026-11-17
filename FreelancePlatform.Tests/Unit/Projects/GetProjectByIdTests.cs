using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Projects.GetProjectById;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Tests.Unit.Projects;

public class GetProjectByIdTests
{
    private readonly TestDb _testDb = new();

    // Анонимный пользователь: у DefaultHttpContext нет аутентифицированной личности
    private static HttpContext Anonymous() => new DefaultHttpContext();

    private Task<IResult> GetAsync(string id, HttpContext ctx, CancellationToken ct) =>
        _testDb.RunAsync(db => GetProjectByIdEndpoint.Handle(id, new GetProjectByIdValidator(), ctx, db, ct));

    [Fact(Timeout = 10_000)]
    public async Task Get_OpenProject_IsPublic_AndHasClientCategoryAndSkills()
    {
        var s = await Seed.DraftProjectAsync(_testDb, status: ProjectStatus.Open, skillsCount: 2);

        var response = await GetAsync(s.ProjectId.ToString(), Anonymous(), TestContext.Current.CancellationToken);

        var result = ResultAssert.Unwrap<ProjectDto>(response);
        Assert.Equal(200, result.Code);
        Assert.Equal(s.ProjectId, result.Value!.Id);
        Assert.Equal("Test User", result.Value.ClientName);
        Assert.False(string.IsNullOrEmpty(result.Value.CategoryName));
        Assert.Equal(2, result.Value.RequiredSkills.Count);
    }

    [Theory(Timeout = 10_000)]
    [InlineData(ProjectStatus.InProgress)]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Cancelled)]
    public async Task Get_NonDraftProject_IsPublic(ProjectStatus status)
    {
        var s = await Seed.DraftProjectAsync(_testDb, status: status);

        var response = await GetAsync(s.ProjectId.ToString(), Anonymous(), TestContext.Current.CancellationToken);

        Assert.Equal(200, ResultAssert.Unwrap<ProjectDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Get_Draft_ByOwner_Returns200()
    {
        var s = await Seed.DraftProjectAsync(_testDb);

        var response = await GetAsync(s.ProjectId.ToString(), TestHttp.As(s.ClientId), TestContext.Current.CancellationToken);

        Assert.Equal(200, ResultAssert.Unwrap<ProjectDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Get_Draft_ByAnotherUser_Returns404_NotForbidden()
    {
        var s = await Seed.DraftProjectAsync(_testDb);

        var response = await GetAsync(s.ProjectId.ToString(), TestHttp.As(Guid.NewGuid()), TestContext.Current.CancellationToken);

        Assert.Equal(404, ResultAssert.Unwrap<ProjectDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Get_Draft_Anonymous_Returns404()
    {
        var s = await Seed.DraftProjectAsync(_testDb);

        var response = await GetAsync(s.ProjectId.ToString(), Anonymous(), TestContext.Current.CancellationToken);

        Assert.Equal(404, ResultAssert.Unwrap<ProjectDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Get_UnknownId_Returns404()
    {
        var response = await GetAsync(Guid.NewGuid().ToString(), Anonymous(), TestContext.Current.CancellationToken);

        Assert.Equal(404, ResultAssert.Unwrap<ProjectDto>(response).Code);
    }

    [Theory(Timeout = 10_000)]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task Get_InvalidId_Returns400(string id)
    {
        var response = await GetAsync(id, Anonymous(), TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<ProjectDto>(response).Code);
    }
}