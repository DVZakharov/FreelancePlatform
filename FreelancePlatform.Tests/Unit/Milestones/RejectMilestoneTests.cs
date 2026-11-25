// Unit/Milestones/RejectMilestoneTests.cs
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Milestones.RejectMilestone;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Milestones;

public class RejectMilestoneTests
{
    private readonly TestDb _testDb = new();

    [Fact(Timeout = 10_000)]
    public async Task Reject_WithReason_ReturnsMilestoneToFunded_AndKeepsMoneyInEscrow()
    {
        var s = await Seed.MilestoneAsync(_testDb, 700, 300, MilestoneStatus.Submitted, withEscrow: true);

        var response = await _testDb.RunAsync(db => RejectMilestoneEndpoint.Handle(
            s.MilestoneId.ToString(),
            new RejectMilestoneRequest("Не соответствует ТЗ"),
            TestHttp.As(s.ClientId),
            new RejectMilestoneValidator(),
            db,
            TestContext.Current.CancellationToken));

        Assert.Equal(200, ResultAssert.Unwrap<MilestoneDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(MilestoneStatus.Funded, (await check.Milestones.FindAsync([s.MilestoneId], TestContext.Current.CancellationToken))!.Status);
        Assert.Equal(HoldStatus.Held, (await check.EscrowHolds.SingleAsync(cancellationToken: TestContext.Current.CancellationToken)).Status);
    }

    [Fact(Timeout = 10_000)]
    public async Task Reject_WithEmptyReason_Returns400_AndChangesNothing()
    {
        var s = await Seed.MilestoneAsync(_testDb, 700, 300, MilestoneStatus.Submitted, withEscrow: true);

        var response = await _testDb.RunAsync(db => RejectMilestoneEndpoint.Handle(
            s.MilestoneId.ToString(),
            new RejectMilestoneRequest(""),
            TestHttp.As(s.ClientId),
            new RejectMilestoneValidator(),
            db,
            TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<MilestoneDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(MilestoneStatus.Submitted, (await check.Milestones.FindAsync([s.MilestoneId], TestContext.Current.CancellationToken))!.Status);
    }
}