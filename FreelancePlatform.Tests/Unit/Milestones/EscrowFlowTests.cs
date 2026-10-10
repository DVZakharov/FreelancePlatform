using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.Features.Milestones.ApproveMilestone;
using FreelancePlatform.Application.Features.Milestones.FundMilestone;
using FreelancePlatform.Application.Features.Milestones.SubmitMilestone;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Milestones;

public class EscrowFlowTests
{
    private readonly TestDb _testDb = new();

    [Fact(Timeout = 10_000)]
    public async Task FundSubmitApprove_ConservesTotalMoney_AndPaysFreelancer()
    {
        var s = await Seed.MilestoneAsync(_testDb, clientBalance: 1000, amount: 300);
        var id = s.MilestoneId.ToString();

        await _testDb.RunAsync(db => FundMilestoneEndpoint.Handle(id, new FundMilestoneValidator(),
            TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));
        Assert.Equal(1000, await _testDb.TotalMoneyAsync());

        await _testDb.RunAsync(db => SubmitMilestoneEndpoint.Handle(id, new SubmitMilestoneValidator(),
            TestHttp.As(s.FreelancerId), db, TestContext.Current.CancellationToken));
        Assert.Equal(1000, await _testDb.TotalMoneyAsync());

        await _testDb.RunAsync(db => ApproveMilestoneEndpoint.Handle(id, new ApproveMilestoneValidator(),
            TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));
        Assert.Equal(1000, await _testDb.TotalMoneyAsync());

        await using var check = _testDb.NewContext();
        Assert.Equal(700, (await check.Users.FindAsync([s.ClientId], TestContext.Current.CancellationToken))!.Balance);
        Assert.Equal(300,
            (await check.Users.FindAsync([s.FreelancerId], TestContext.Current.CancellationToken))!.Balance);
        Assert.Equal(HoldStatus.Released,
            (await check.EscrowHolds.SingleAsync(cancellationToken: TestContext.Current.CancellationToken)).Status);
        Assert.Equal(MilestoneStatus.Completed,
            (await check.Milestones.FindAsync([s.MilestoneId], TestContext.Current.CancellationToken))!.Status);
    }

    [Fact(Timeout = 10_000)]
    public async Task Approve_ByFreelancer_Returns403_AndPaysNothing()
    {
        var s = await Seed.MilestoneAsync(_testDb, 700, 300, MilestoneStatus.Submitted, withEscrow: true);

        await _testDb.RunAsync(db =>
            ApproveMilestoneEndpoint.Handle(s.MilestoneId.ToString(), new ApproveMilestoneValidator(),
                TestHttp.As(s.FreelancerId), db, TestContext.Current.CancellationToken));

        await using var check = _testDb.NewContext();
        Assert.Equal(0,
            (await check.Users.FindAsync([s.FreelancerId], TestContext.Current.CancellationToken))!.Balance);
        Assert.Equal(HoldStatus.Held,
            (await check.EscrowHolds.SingleAsync(cancellationToken: TestContext.Current.CancellationToken)).Status);
    }
}