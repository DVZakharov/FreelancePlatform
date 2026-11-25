using FreelancePlatform.Application.Features.Proposals.RejectProposal;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Proposals;

public class RejectProposalTests
{
    private readonly TestDb _testDb = new();
    
    private async Task<Guid> AddProposalAsync(Guid projectId, Guid freelancerId, ProposalStatus status = ProposalStatus.Pending)
    {
        await using var db = _testDb.NewContext();
        var proposal = new ProposalEntity
        {
            ProjectId = projectId, FreelancerId = freelancerId, Price = 100, EstimatedDays = 5,
            CoverLetter = "Готов взяться за работу", Status = status
        };
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return proposal.Id;
    }
    
    private async Task<Guid> AddFreelancerAsync()
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser(role: UserRole.Client | UserRole.Freelancer);
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user.Id;
    }

    private Task<Microsoft.AspNetCore.Http.IResult> RejectAsync(string id, Guid userId, CancellationToken ct) =>
        _testDb.RunAsync(db => RejectProposalEndpoint.Handle(
            id, new RejectProposalValidator(), TestHttp.As(userId), db, ct));

    [Fact(Timeout = 10_000)]
    public async Task Reject_ByOwner_RejectsOnlyThatProposal_AndKeepsProjectOpen()
    {
        var s = await Seed.OpenProjectAsync(_testDb);
        var target = await AddProposalAsync(s.ProjectId, s.FreelancerId);
        var untouched = await AddProposalAsync(s.ProjectId, await AddFreelancerAsync());

        var response = await RejectAsync(target.ToString(), s.ClientId, TestContext.Current.CancellationToken);

        Assert.Equal(200, ResultAssert.Unwrap<object>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal(ProposalStatus.Rejected, (await check.Proposals.SingleAsync(p => p.Id == target, TestContext.Current.CancellationToken)).Status);
        Assert.Equal(ProposalStatus.Pending, (await check.Proposals.SingleAsync(p => p.Id == untouched, TestContext.Current.CancellationToken)).Status);
        Assert.Equal(ProjectStatus.Open, (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Fact(Timeout = 10_000)]
    public async Task Reject_ByNonOwner_Returns403_AndKeepsPending()
    {
        var s = await Seed.OpenProjectAsync(_testDb);
        var proposal = await AddProposalAsync(s.ProjectId, s.FreelancerId);

        var response = await RejectAsync(proposal.ToString(), s.FreelancerId, TestContext.Current.CancellationToken);

        Assert.Equal(403, ResultAssert.Unwrap<object>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal(ProposalStatus.Pending, (await check.Proposals.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Theory(Timeout = 10_000)]
    [InlineData(ProposalStatus.Accepted)]
    [InlineData(ProposalStatus.Rejected)]
    public async Task Reject_NotPendingProposal_Returns409_AndKeepsStatus(ProposalStatus status)
    {
        var s = await Seed.OpenProjectAsync(_testDb);
        var proposal = await AddProposalAsync(s.ProjectId, s.FreelancerId, status);

        var response = await RejectAsync(proposal.ToString(), s.ClientId, TestContext.Current.CancellationToken);

        Assert.Equal(409, ResultAssert.Unwrap<object>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal(status, (await check.Proposals.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Fact(Timeout = 10_000)]
    public async Task Reject_Unknown_Returns404()
    {
        var response = await RejectAsync(Guid.NewGuid().ToString(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(404, ResultAssert.Unwrap<object>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Reject_InvalidId_Returns400()
    {
        var response = await RejectAsync("not-a-guid", Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<object>(response).Code);
    }
}