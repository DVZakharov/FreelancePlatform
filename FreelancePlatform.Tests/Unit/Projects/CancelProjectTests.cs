using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.Features.Projects.CancelProject;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Projects;

public class CancelProjectTests
{
    private readonly TestDb _testDb = new();

    private static ProposalEntity Proposal(Guid projectId, ProposalStatus status) => new()
    {
        ProjectId = projectId, FreelancerId = Guid.NewGuid(), Price = 1, EstimatedDays = 1, Status = status
    };

    [Fact(Timeout = 10_000)]
    public async Task Cancel_DraftProject_Succeeds()
    {
        var s = await Seed.DraftProjectAsync(_testDb);

        var response = await _testDb.RunAsync(db => CancelProjectEndpoint.Handle(
            s.ProjectId.ToString(), new CancelProjectValidator(), TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(ProjectStatus.Cancelled, (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }
    
    [Theory(Timeout = 10_000)]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task Cancel_WithInvalidId_Returns400(string id)
    {
        var response = await _testDb.RunAsync(db => CancelProjectEndpoint.Handle(
            id, new CancelProjectValidator(), TestHttp.As(Guid.NewGuid()), db, TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<object>(response).Code);
    }
    
    [Fact(Timeout = 10_000)]
    public async Task Cancel_OpenProject_RejectsPendingProposals_AndLeavesProcessedOnes()
    {
        var s = await Seed.OpenProjectAsync(_testDb);

        await using (var db = _testDb.NewContext())
        {
            db.Proposals.AddRange(
                Proposal(s.ProjectId, ProposalStatus.Pending),
                Proposal(s.ProjectId, ProposalStatus.Pending),
                Proposal(s.ProjectId, ProposalStatus.Accepted));
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _testDb.RunAsync(db => CancelProjectEndpoint.Handle(
            s.ProjectId.ToString(), new CancelProjectValidator(), TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(ProjectStatus.Cancelled, (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Status);
        Assert.Equal(2, await check.Proposals.CountAsync(p => p.Status == ProposalStatus.Rejected, TestContext.Current.CancellationToken));
        Assert.Equal(1, await check.Proposals.CountAsync(p => p.Status == ProposalStatus.Accepted, TestContext.Current.CancellationToken));
    }

    [Fact(Timeout = 10_000)]
    public async Task Cancel_InProgressProject_Returns409_AndChangesNothing()
    {
        var s = await Seed.OpenProjectAsync(_testDb, status: ProjectStatus.InProgress);

        var response = await _testDb.RunAsync(db => CancelProjectEndpoint.Handle(
            s.ProjectId.ToString(), new CancelProjectValidator(), TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(409, ResultAssert.Unwrap<object>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(ProjectStatus.InProgress, (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Fact(Timeout = 10_000)]
    public async Task Cancel_ByNonOwner_Returns403_AndChangesNothing()
    {
        var s = await Seed.OpenProjectAsync(_testDb);

        var response = await _testDb.RunAsync(db => CancelProjectEndpoint.Handle(
            s.ProjectId.ToString(), new CancelProjectValidator(), TestHttp.As(s.FreelancerId), db, TestContext.Current.CancellationToken));

        Assert.Equal(403, ResultAssert.Unwrap<object>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(ProjectStatus.Open, (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }
}