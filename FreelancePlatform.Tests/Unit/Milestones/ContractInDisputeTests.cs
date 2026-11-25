using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Milestones.ApproveMilestone;
using FreelancePlatform.Application.Features.Milestones.SubmitMilestone;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Milestones;

public class ContractInDisputeTests
{
    private readonly TestDb _testDb = new();

    [Fact(Timeout = 10_000)]
    public async Task Approve_WhenContractInDispute_Returns409()
    {
        var s = await Seed.MilestoneAsync(_testDb, 700, 300, MilestoneStatus.Submitted,
            ContractStatus.InDispute, withEscrow: true);

        var response = await _testDb.RunAsync(db =>
            ApproveMilestoneEndpoint.Handle(s.MilestoneId.ToString(), new ApproveMilestoneValidator(),TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(409, ResultAssert.Unwrap<MilestoneDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Submit_WhenContractInDispute_Returns409()
    {
        var s = await Seed.MilestoneAsync(_testDb, 700, 300, MilestoneStatus.Funded,
            ContractStatus.InDispute, withEscrow: true);

        var response = await _testDb.RunAsync(db =>
            SubmitMilestoneEndpoint.Handle(s.MilestoneId.ToString(), new SubmitMilestoneValidator(),TestHttp.As(s.FreelancerId), db, TestContext.Current.CancellationToken));

        Assert.Equal(409, ResultAssert.Unwrap<MilestoneDto>(response).Code);
    }
}