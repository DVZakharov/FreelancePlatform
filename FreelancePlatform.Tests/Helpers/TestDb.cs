using FreelancePlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Infrastructure.Data;

namespace FreelancePlatform.Tests.Helpers;

public sealed class TestDb
{
    private readonly string _name = Guid.NewGuid().ToString();

    // Каждый вызов даёт НОВЫЙ контекст на одной и той же in-memory базе
    public AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(_name).Options);

    public async Task<T> RunAsync<T>(Func<AppDbContext, Task<T>> action)
    {
        await using var db = NewContext();
        return await action(db);
    }
    
    public async Task<decimal> TotalMoneyAsync()
    {
        await using var db = NewContext();
        var ct = TestContext.Current.CancellationToken;
        var balances = await db.Users.SumAsync(u => u.Balance, ct);
        var held = await db.EscrowHolds.Where(e => e.Status == HoldStatus.Held).SumAsync(e => e.Amount, ct);
        return balances + held;
    }
}