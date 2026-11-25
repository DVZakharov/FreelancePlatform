using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Payments.TopUpBalance;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Payments;

public class TopUpBalanceTests
{
    private readonly TestDb _testDb = new();

    private async Task<Guid> SeedUserAsync(decimal balance)
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser(balance);
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user.Id;
    }

    [Fact(Timeout = 10_000)]
    public async Task TopUp_AddsToBalance_AndWritesTransaction()
    {
        var userId = await SeedUserAsync(100);

        var response = await _testDb.RunAsync(db => TopUpBalanceEndpoint.Handle(
            new TopUpBalanceRequest(250), TestHttp.As(userId), new TopUpBalanceValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(201, ResultAssert.Unwrap<TransactionDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(350, (await check.Users.SingleAsync(TestContext.Current.CancellationToken)).Balance);

        var tx = await check.Transactions.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(TransactionType.TopUp, tx.Type);
        Assert.Equal(250, tx.Amount);
        Assert.Equal(350, tx.BalanceAfter);
    }

    [Theory(Timeout = 10_000)]
    [InlineData(0)]
    [InlineData(-50)]
    [InlineData(1_000_001)]
    public async Task TopUp_WithInvalidAmount_Returns400_AndChangesNothing(int amount)
    {
        var userId = await SeedUserAsync(100);

        var response = await _testDb.RunAsync(db => TopUpBalanceEndpoint.Handle(
            new TopUpBalanceRequest(amount), TestHttp.As(userId), new TopUpBalanceValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<TransactionDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(100, (await check.Users.SingleAsync(TestContext.Current.CancellationToken)).Balance);
        Assert.Empty(check.Transactions);
    }

    [Fact(Timeout = 10_000)]
    public async Task TopUp_MaximumAllowedAmount_IsAccepted()
    {
        var userId = await SeedUserAsync(0);

        var response = await _testDb.RunAsync(db => TopUpBalanceEndpoint.Handle(
            new TopUpBalanceRequest(1_000_000), TestHttp.As(userId), new TopUpBalanceValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(201, ResultAssert.Unwrap<TransactionDto>(response).Code);
    }
}