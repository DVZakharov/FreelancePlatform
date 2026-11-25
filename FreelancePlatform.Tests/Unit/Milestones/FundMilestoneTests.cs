using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Milestones.FundMilestone;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Milestones;

public class FundMilestoneTests
{
    private readonly TestDb _testDb = new();

    [Fact(Timeout = 10_000)]
    public async Task Fund_WithEnoughBalance_FreezesMoneyInEscrow()
    {
        var s = await Seed.MilestoneAsync(_testDb, clientBalance: 1000, amount: 300);

        var response = await _testDb.RunAsync(db =>
            FundMilestoneEndpoint.Handle(s.MilestoneId.ToString(), new FundMilestoneValidator() , TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(200, ResultAssert.Unwrap<MilestoneDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(700, (await check.Users.FindAsync([s.ClientId], TestContext.Current.CancellationToken))!.Balance);

        var hold = await check.EscrowHolds.SingleAsync(e => e.MilestoneId == s.MilestoneId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HoldStatus.Held, hold.Status);
        Assert.Equal(300, hold.Amount);

        Assert.Equal(MilestoneStatus.Funded, (await check.Milestones.FindAsync([s.MilestoneId], TestContext.Current.CancellationToken))!.Status);

        var tx = await check.Transactions.SingleAsync(t => t.UserId == s.ClientId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(TransactionType.Freeze, tx.Type);
        Assert.Equal(700, tx.BalanceAfter);
    }

    [Fact(Timeout = 10_000)]
    public async Task Fund_WithInsufficientBalance_Returns400_AndChangesNothing()
    {
        var s = await Seed.MilestoneAsync(_testDb, clientBalance: 100, amount: 300);

        var response = await _testDb.RunAsync(db =>
            FundMilestoneEndpoint.Handle(s.MilestoneId.ToString(), new FundMilestoneValidator(), TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<MilestoneDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(100, (await check.Users.FindAsync([s.ClientId], TestContext.Current.CancellationToken))!.Balance);
        Assert.Empty(check.EscrowHolds);
        Assert.Empty(check.Transactions);
    }

    [Fact(Timeout = 10_000)]
    public async Task Fund_ByFreelancer_Returns403()
    {
        var s = await Seed.MilestoneAsync(_testDb, clientBalance: 1000, amount: 300);

        var response = await _testDb.RunAsync(db =>
            FundMilestoneEndpoint.Handle(s.MilestoneId.ToString(), new FundMilestoneValidator(), TestHttp.As(s.FreelancerId), db, TestContext.Current.CancellationToken));

        Assert.Equal(403, ResultAssert.Unwrap<MilestoneDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Fund_AlreadyFundedMilestone_Returns409()
    {
        var s = await Seed.MilestoneAsync(_testDb, 1000, 300, milestoneStatus: MilestoneStatus.Funded, withEscrow: true);

        var response = await _testDb.RunAsync(db =>
            FundMilestoneEndpoint.Handle(s.MilestoneId.ToString(), new FundMilestoneValidator(), TestHttp.As(s.ClientId), db, TestContext.Current.CancellationToken));

        Assert.Equal(409, ResultAssert.Unwrap<MilestoneDto>(response).Code);
    }
}