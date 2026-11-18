using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Proposals.GetMyProposals;
using FreelancePlatform.Application.Features.Proposals.GetProposalsForProject;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Proposals;

public class ProposalReadTests
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

    // ---- GetProposalsForProject ----

    private Task<Microsoft.AspNetCore.Http.IResult> ForProjectAsync(string projectId, Guid userId, CancellationToken ct) =>
        _testDb.RunAsync(db => GetProposalsForProjectEndpoint.Handle(
            projectId, new GetProposalsForProjectValidator(), TestHttp.As(userId), db, ct));

    [Fact(Timeout = 10_000)]
    public async Task ForProject_Owner_SeesAllProposalsOfThatProjectOnly()
    {
        var s = await Seed.OpenProjectAsync(_testDb);
        var other = await Seed.OpenProjectAsync(_testDb);
        await AddProposalAsync(s.ProjectId, s.FreelancerId);
        await AddProposalAsync(s.ProjectId, await AddFreelancerAsync(), ProposalStatus.Rejected);
        await AddProposalAsync(other.ProjectId, other.FreelancerId);

        var result = ResultAssert.Unwrap<List<ProposalDto>>(await ForProjectAsync(s.ProjectId.ToString(), s.ClientId, TestContext.Current.CancellationToken));

        Assert.Equal(200, result.Code);
        Assert.Equal(2, result.Value!.Count);
        Assert.All(result.Value, p => Assert.Equal(s.ProjectId, p.ProjectId));
    }

    [Fact(Timeout = 10_000)]
    public async Task ForProject_NonOwner_EvenTheApplicant_Returns403()
    {
        var s = await Seed.OpenProjectAsync(_testDb);
        await AddProposalAsync(s.ProjectId, s.FreelancerId);

        var response = await ForProjectAsync(s.ProjectId.ToString(), s.FreelancerId, TestContext.Current.CancellationToken);

        Assert.Equal(403, ResultAssert.Unwrap<List<ProposalDto>>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task ForProject_Unknown_Returns404()
    {
        var response = await ForProjectAsync(Guid.NewGuid().ToString(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(404, ResultAssert.Unwrap<List<ProposalDto>>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task ForProject_InvalidId_Returns400()
    {
        var response = await ForProjectAsync("not-a-guid", Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<List<ProposalDto>>(response).Code);
    }

    // ---- GetMyProposals ----

    [Fact(Timeout = 10_000)]
    public async Task GetMy_ReturnsOnlyMyProposals_InAnyStatus()
    {
        var s = await Seed.OpenProjectAsync(_testDb);
        var second = await Seed.OpenProjectAsync(_testDb);
        await AddProposalAsync(s.ProjectId, s.FreelancerId, ProposalStatus.Pending);
        await AddProposalAsync(second.ProjectId, s.FreelancerId, ProposalStatus.Rejected);
        await AddProposalAsync(s.ProjectId, await AddFreelancerAsync());     // чужой отклик

        var response = await _testDb.RunAsync(db => GetMyProposalsEndpoint.Handle(TestHttp.As(s.FreelancerId), db, TestContext.Current.CancellationToken));

        var result = ResultAssert.Unwrap<List<ProposalDto>>(response);
        Assert.Equal(2, result.Value!.Count);
        Assert.All(result.Value, p => Assert.Equal(s.FreelancerId, p.FreelancerId));
    }
}
