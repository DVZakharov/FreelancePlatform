using FreelancePlatform.Application.Features.Payments.GetMyBalance;
using FreelancePlatform.Application.Features.Payments.GetTransactionHistory;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Payments;

public class TransactionHistoryTests
{
    private readonly TestDb _testDb = new();

    private async Task<Guid> SeedUserWithTransactionsAsync(CancellationToken ct,
        params (TransactionType Type, decimal Amount, int MinutesAgo)[] transactions)
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser();
        db.Users.Add(user);
        foreach (var (type, amount, minutesAgo) in transactions)
            db.Transactions.Add(new TransactionEntity
            {
                UserId = user.Id, Type = type, Amount = amount, BalanceAfter = amount,
                Description = $"{type} {amount}", CreatedAt = DateTime.UtcNow.AddMinutes(-minutesAgo)
            });
        await db.SaveChangesAsync(ct);
        return user.Id;
    }

    private async Task<GetTransactionHistoryResponse> HistoryAsync(Guid userId, GetTransactionHistoryRequest request)
    {
        var response = await _testDb.RunAsync(db => GetTransactionHistoryEndpoint.Handle(
            request, new GetTransactionHistoryValidator(), TestHttp.As(userId), db, TestContext.Current.CancellationToken));
        var result = ResultAssert.Unwrap<GetTransactionHistoryResponse>(response);
        Assert.Equal(200, result.Code);
        return result.Value!;
    }

    [Fact(Timeout = 10_000)]
    public async Task History_ReturnsOnlyMyTransactions_NewestFirst()
    {
        var me = await SeedUserWithTransactionsAsync(TestContext.Current.CancellationToken,
            (TransactionType.TopUp, 100, 30), (TransactionType.Freeze, 40, 10), (TransactionType.Release, 40, 20));
        await SeedUserWithTransactionsAsync(TestContext.Current.CancellationToken, (TransactionType.TopUp, 999, 5)); // чужая

        var history = await HistoryAsync(me, new GetTransactionHistoryRequest());

        Assert.Equal(3, history.TotalCount);
        Assert.Equal(new[] { "Freeze", "Release", "TopUp" }, history.Items.Select(i => i.Type).ToArray());
        Assert.DoesNotContain(history.Items, i => i.Amount == 999);
    }

    [Theory(Timeout = 10_000)]
    [InlineData("Freeze")]
    [InlineData("freeze")]
    [InlineData("FREEZE")]
    public async Task History_FiltersByType_IgnoringCase(string type)
    {
        var me = await SeedUserWithTransactionsAsync(TestContext.Current.CancellationToken,
            (TransactionType.TopUp, 100, 30), (TransactionType.Freeze, 40, 10), (TransactionType.Freeze, 60, 20));

        var history = await HistoryAsync(me, new GetTransactionHistoryRequest { Type = type });

        Assert.Equal(2, history.TotalCount);
        Assert.All(history.Items, i => Assert.Equal("Freeze", i.Type));
    }

    [Fact(Timeout = 10_000)]
    public async Task History_Paginates_AndTotalIgnoresPageSize()
    {
        var me = await SeedUserWithTransactionsAsync(TestContext.Current.CancellationToken,
            [.. Enumerable.Range(1, 5).Select(i => (TransactionType.TopUp, (decimal)i, i))]);

        var history = await HistoryAsync(me, new GetTransactionHistoryRequest { PageNumber = 2, PageSize = 2 });

        Assert.Equal(5, history.TotalCount);
        Assert.Equal(2, history.Page);
        Assert.Equal(2, history.PageSize);
        // от новых к старым: 1 мин, 2, | 3, 4 | 5 — вторая страница = суммы 3 и 4
        Assert.Equal(new[] { 3m, 4m }, history.Items.Select(i => i.Amount).ToArray());
    }

    [Fact(Timeout = 10_000)]
    public async Task History_Empty_Returns200WithNoItems()
    {
        var me = await SeedUserWithTransactionsAsync(TestContext.Current.CancellationToken);

        var history = await HistoryAsync(me, new GetTransactionHistoryRequest());

        Assert.Empty(history.Items);
        Assert.Equal(0, history.TotalCount);
    }

    [Theory(Timeout = 10_000)]
    [InlineData("Withdraw", 1, 10)]
    [InlineData(null, 0, 10)]
    [InlineData(null, 1, 0)]
    public async Task History_InvalidRequest_Returns400(string? type, int page, int size)
    {
        var me = await SeedUserWithTransactionsAsync(TestContext.Current.CancellationToken);

        var response = await _testDb.RunAsync(db => GetTransactionHistoryEndpoint.Handle(
            new GetTransactionHistoryRequest { Type = type, PageNumber = page, PageSize = size },
            new GetTransactionHistoryValidator(), TestHttp.As(me), db, TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<GetTransactionHistoryResponse>(response).Code);
    }

    // ---- Баланс ----

    [Fact(Timeout = 10_000)]
    public async Task Balance_ReturnsCurrentBalanceOfCaller()
    {
        Guid me;
        await using (var db = _testDb.NewContext())
        {
            var user = Seed.NewUser(balance: 123.45m);
            var other = Seed.NewUser(balance: 999);
            db.AddRange(user, other);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            me = user.Id;
        }

        var response = await _testDb.RunAsync(db => GetMyBalanceEndpoint.Handle(TestHttp.As(me), db, TestContext.Current.CancellationToken));

        var result = ResultAssert.Unwrap<GetMyBalanceResponse>(response);
        Assert.Equal(200, result.Code);
        Assert.Equal(123.45m, result.Value!.Balance);
    }
}