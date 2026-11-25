// Unit/Disputes/ResolveDisputeTests.cs
using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Disputes.ResolveDispute;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Tests.Unit.Disputes;

public class ResolveDisputeTests
{
    private readonly TestDb _testDb = new();

    private Task<IResult> ResolveAsync(DisputeScenario s, Guid actorId, string decision) =>
        _testDb.RunAsync(db => ResolveDisputeEndpoint.Handle(
            s.DisputeId.ToString(),
            new ResolveDisputeRequest(decision, "Решение администратора"),
            TestHttp.As(actorId),
            new ResolveDisputeValidator(),
            db, TestContext.Current.CancellationToken));

    [Fact(Timeout = 10_000)]
    public async Task Resolve_ForClient_RefundsAllHeldEscrows_AndReactivatesContract()
    {
        var s = await Seed.DisputeAsync(_testDb, clientBalance: 500, 300, 200);

        var response = await ResolveAsync(s, s.AdminId, "Client");

        Assert.Equal(200, ResultAssert.Unwrap<DisputeDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(1000, (await check.Users.FindAsync([s.ClientId], TestContext.Current.CancellationToken))!.Balance);
        Assert.Equal(0, (await check.Users.FindAsync([s.FreelancerId], TestContext.Current.CancellationToken))!.Balance);
        Assert.All(check.EscrowHolds, h => Assert.Equal(HoldStatus.Refunded, h.Status));
        Assert.Equal(2, await check.Transactions.CountAsync(t => t.Type == TransactionType.Refund, TestContext.Current.CancellationToken));

        var dispute = await check.Disputes.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DisputeStatus.ResolvedForClient, dispute.Status);
        Assert.Equal(s.AdminId, dispute.ResolvedById);

        Assert.Equal(ContractStatus.Active, (await check.Contracts.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Fact(Timeout = 10_000)]
    public async Task Resolve_ForFreelancer_ReleasesEscrows_AndCompletesMilestones()
    {
        var s = await Seed.DisputeAsync(_testDb, clientBalance: 500, 300, 200);

        await ResolveAsync(s, s.AdminId, "Freelancer");

        await using var check = _testDb.NewContext();
        Assert.Equal(500, (await check.Users.FindAsync([s.ClientId], TestContext.Current.CancellationToken))!.Balance);
        Assert.Equal(500, (await check.Users.FindAsync([s.FreelancerId], TestContext.Current.CancellationToken))!.Balance);
        Assert.All(check.EscrowHolds, h => Assert.Equal(HoldStatus.Released, h.Status));
        Assert.All(check.Milestones, m => Assert.Equal(MilestoneStatus.Completed, m.Status));
        Assert.Equal(DisputeStatus.ResolvedForFreelancer, (await check.Disputes.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Theory]
    [InlineData("Client")]
    [InlineData("Freelancer")]
    public async Task Resolve_ConservesTotalMoney(string decision)
    {
        var s = await Seed.DisputeAsync(_testDb, clientBalance: 500, 300, 200);
        var before = await _testDb.TotalMoneyAsync();

        await ResolveAsync(s, s.AdminId, decision);

        Assert.Equal(before, await _testDb.TotalMoneyAsync());
    }

    [Fact(Timeout = 10_000)]
    public async Task Resolve_AlreadyResolvedDispute_Returns409_AndDoesNotPayTwice()
    {
        var s = await Seed.DisputeAsync(_testDb, clientBalance: 500, 300);

        await ResolveAsync(s, s.AdminId, "Client");
        var second = await ResolveAsync(s, s.AdminId, "Client");

        Assert.Equal(409, ResultAssert.Unwrap<DisputeDto>(second).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(800, (await check.Users.FindAsync([s.ClientId], TestContext.Current.CancellationToken))!.Balance);
    }

    [Fact]
    public async Task Resolve_WithInvalidDecision_Returns400()
    {
        var s = await Seed.DisputeAsync(_testDb, clientBalance: 500, 300);

        var response = await ResolveAsync(s, s.AdminId, "Nobody");

        Assert.Equal(400, ResultAssert.Unwrap<DisputeDto>(response).Code);
    }
}