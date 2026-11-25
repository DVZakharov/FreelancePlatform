using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Proposals.AcceptProposal;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Proposals;

public class AcceptProposalTests
{
    private readonly TestDb _testDb = new();

    private async Task<(Guid ClientId, Guid ProjectId, List<ProposalEntity> Proposals)> SeedOpenProjectAsync(
        int proposalsCount = 3)
    {
        await using var db = _testDb.NewContext();

        var client = Seed.NewUser();
        var project = new ProjectEntity
        {
            ClientId = client.Id, Title = "Project", Description = "Description", Status = ProjectStatus.Open
        };
        db.AddRange(client, project);

        var proposals = new List<ProposalEntity>();
        for (var i = 0; i < proposalsCount; i++)
        {
            var freelancer = Seed.NewUser();
            var proposal = new ProposalEntity
            {
                ProjectId = project.Id, FreelancerId = freelancer.Id,
                Price = 100 + i * 50, EstimatedDays = 5, CoverLetter = "hi"
            };
            db.AddRange(freelancer, proposal);
            proposals.Add(proposal);
        }

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (client.Id, project.Id, proposals);
    }

    [Fact(Timeout = 10_000)]
    public async Task Accept_RejectsOthers_StartsProject_AndCreatesContract()
    {
        var (clientId, projectId, proposals) = await SeedOpenProjectAsync();
        var chosen = proposals[1];

        var response = await _testDb.RunAsync(db =>
            AcceptProposalEndpoint.Handle(chosen.Id.ToString(), new AcceptProposalValidator(), TestHttp.As(clientId),
                db, CancellationToken.None));

        Assert.Equal(201, ResultAssert.Unwrap<ContractDto>(response).Code);

        await using var check = _testDb.NewContext();
        var statuses = await check.Proposals.ToDictionaryAsync(p => p.Id, p => p.Status,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(ProposalStatus.Accepted, statuses[chosen.Id]);
        Assert.All(proposals.Where(p => p.Id != chosen.Id), p => Assert.Equal(ProposalStatus.Rejected, statuses[p.Id]));

        var project = await check.Projects.FindAsync([projectId], TestContext.Current.CancellationToken);
        Assert.Equal(ProjectStatus.InProgress, project!.Status);
        Assert.Equal(chosen.Id, project.SelectedProposalId);

        var contract = await check.Contracts.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(chosen.FreelancerId, contract.FreelancerId);
        Assert.Equal(chosen.Price, contract.AgreedPrice);
        Assert.Equal(ContractStatus.Active, contract.Status);
    }

    [Fact]
    public async Task Accept_ByNonOwner_Returns403_AndChangesNothing()
    {
        var (_, _, proposals) = await SeedOpenProjectAsync();

        var response = await _testDb.RunAsync(db =>
            AcceptProposalEndpoint.Handle(proposals[0].Id.ToString(), new AcceptProposalValidator(),
                TestHttp.As(Guid.NewGuid()), db, CancellationToken.None));

        Assert.Equal(403, ResultAssert.Unwrap<ContractDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Empty(check.Contracts);
        Assert.All(check.Proposals, p => Assert.Equal(ProposalStatus.Pending, p.Status));
    }

    [Fact(Timeout = 10_000)]
    public async Task Accept_SecondProposalOnSameProject_Returns409()
    {
        var (clientId, _, proposals) = await SeedOpenProjectAsync();

        await _testDb.RunAsync(db =>
            AcceptProposalEndpoint.Handle(proposals[0].Id.ToString(), new AcceptProposalValidator(), TestHttp.As(clientId), db,
                TestContext.Current.CancellationToken));

        var second = await _testDb.RunAsync(db =>
            AcceptProposalEndpoint.Handle(proposals[1].Id.ToString(), new AcceptProposalValidator(),TestHttp.As(clientId), db,
                TestContext.Current.CancellationToken));

        Assert.Equal(409, ResultAssert.Unwrap<ContractDto>(second).Code);

        await using var check = _testDb.NewContext();
        Assert.Single(check.Contracts);
    }
}