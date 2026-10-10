using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Projects.PublishProject;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Projects;

public class PublishProjectTests
{
    private readonly TestDb _testDb = new();

    [Fact(Timeout = 10_000)]
    public async Task Publish_ValidDraft_OpensProject_AndSetsPublishedAt()
    {
        var s = await Seed.DraftProjectAsync(_testDb);

        var response = await _testDb.RunAsync(db => PublishProjectEndpoint.Handle(
            s.ProjectId.ToString(), new PublishProjectValidator(), TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(200, ResultAssert.Unwrap<ProjectDto>(response).Code);

        await using var check = _testDb.NewContext();
        var project = await check.Projects.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ProjectStatus.Open, project.Status);
        Assert.NotNull(project.PublishedAt);
    }

    [Theory(Timeout = 10_000)]
    //           title  descLen  min  max  days  skills
    [InlineData("",     100,     100, 500, 7,    1)]   // нет названия
    [InlineData("T",    49,      100, 500, 7,    1)]   // описание короче 50
    [InlineData("T",    100,     0,   500, 7,    1)]   // нулевой минимум бюджета
    [InlineData("T",    100,     100, 0,   7,    1)]   // нулевой максимум
    [InlineData("T",    100,     100, 500, -1,   1)]   // дедлайн в прошлом
    [InlineData("T",    100,     100, 500, 7,    0)]   // нет навыков
    public async Task Publish_IncompleteDraft_Returns400_AndStaysDraft(
        string title, int descLength, decimal min, decimal max, int days, int skills)
    {
        var s = await Seed.DraftProjectAsync(_testDb, title, descLength, min, max, days, skills);

        var response = await _testDb.RunAsync(db => PublishProjectEndpoint.Handle(
            s.ProjectId.ToString(), new PublishProjectValidator(), TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<ProjectDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(ProjectStatus.Draft, (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Fact(Timeout = 10_000)]
    public async Task Publish_ByNonOwner_Returns403()
    {
        var s = await Seed.DraftProjectAsync(_testDb);

        var response = await _testDb.RunAsync(db => PublishProjectEndpoint.Handle(
            s.ProjectId.ToString(), new PublishProjectValidator(), TestHttp.As(Guid.NewGuid()),  db, TestContext.Current.CancellationToken));

        Assert.Equal(403, ResultAssert.Unwrap<ProjectDto>(response).Code);
    }

    [Theory(Timeout = 10_000)]
    [InlineData(ProjectStatus.Open)]
    [InlineData(ProjectStatus.InProgress)]
    [InlineData(ProjectStatus.Cancelled)]
    public async Task Publish_NonDraft_Returns409(ProjectStatus status)
    {
        var s = await Seed.DraftProjectAsync(_testDb, status: status);

        var response = await _testDb.RunAsync(db => PublishProjectEndpoint.Handle(
            s.ProjectId.ToString(), new PublishProjectValidator(),TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(409, ResultAssert.Unwrap<ProjectDto>(response).Code);
    }
}