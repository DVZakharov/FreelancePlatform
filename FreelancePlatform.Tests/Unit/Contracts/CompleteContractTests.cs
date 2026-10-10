using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Contracts.CompleteContract;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Contracts;

public class CompleteContractTests
{
    private readonly TestDb _testDb = new();

    private async Task AddMilestoneAsync(Guid contractId, MilestoneStatus status)
    {
        await using var db = _testDb.NewContext();
        db.Milestones.Add(new MilestoneEntity { ContractId = contractId, Title = "Extra", Amount = 10, Status = status });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Timeout = 10_000)]
    public async Task Complete_WhenAllMilestonesCompleted_CompletesContractAndProject()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 300, MilestoneStatus.Completed);
        await AddMilestoneAsync(s.ContractId, MilestoneStatus.Completed);

        var response = await _testDb.RunAsync(db => CompleteContractEndpoint.Handle(
            s.ContractId.ToString(), new CompleteContractValidator(),TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(200, ResultAssert.Unwrap<ContractDto>(response).Code);

        await using var check = _testDb.NewContext();
        var contract = await check.Contracts.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ContractStatus.Completed, contract.Status);
        Assert.NotNull(contract.CompletedAt);
        Assert.Equal(ProjectStatus.Completed, (await check.Projects.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Theory(Timeout = 10_000)]
    [InlineData(MilestoneStatus.Pending)]
    [InlineData(MilestoneStatus.Funded)]
    [InlineData(MilestoneStatus.Submitted)]
    public async Task Complete_WithUnfinishedMilestone_Returns409(MilestoneStatus unfinished)
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 300, MilestoneStatus.Completed);
        await AddMilestoneAsync(s.ContractId, unfinished);

        var response = await _testDb.RunAsync(db => CompleteContractEndpoint.Handle(
            s.ContractId.ToString(), new CompleteContractValidator(),TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(409, ResultAssert.Unwrap<ContractDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(ContractStatus.Active, (await check.Contracts.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Fact(Timeout = 10_000)]
    public async Task Complete_ByFreelancer_Returns403()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 300, MilestoneStatus.Completed);

        var response = await _testDb.RunAsync(db => CompleteContractEndpoint.Handle(
            s.ContractId.ToString(), new CompleteContractValidator(), TestHttp.As(s.FreelancerId), db, TestContext.Current.CancellationToken));

        Assert.Equal(403, ResultAssert.Unwrap<ContractDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Complete_InDispute_Returns409()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 300, MilestoneStatus.Completed, ContractStatus.InDispute);

        var response = await _testDb.RunAsync(db => CompleteContractEndpoint.Handle(
            s.ContractId.ToString(), new CompleteContractValidator(), TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(409, ResultAssert.Unwrap<ContractDto>(response).Code);
    }
}