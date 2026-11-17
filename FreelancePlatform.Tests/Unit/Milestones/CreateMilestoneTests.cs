using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Milestones.CreateMilestone;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Milestones;

public class CreateMilestoneTests
{
    private readonly TestDb _testDb = new();

    private Task<Microsoft.AspNetCore.Http.IResult> CreateAsync(string contractId, CreateMilestoneRequest request, Guid userId, CancellationToken ct) =>
        _testDb.RunAsync(db => CreateMilestoneEndpoint.Handle(
            contractId, request, TestHttp.As(userId), new CreateMilestoneValidator(), db, ct));

    private static CreateMilestoneRequest Valid() => new("Вёрстка", 300, DateTime.UtcNow.AddDays(5));

    [Fact(Timeout = 10_000)]
    public async Task Create_ByClient_AddsPendingMilestone()
    {
        var s = await Seed.MilestoneAsync(_testDb, clientBalance: 0, amount: 100, agreedPrice: 1000);

        var response = await CreateAsync(s.ContractId.ToString(), Valid(), s.ClientId, TestContext.Current.CancellationToken);

        var result = ResultAssert.Unwrap<MilestoneDto>(response);
        Assert.Equal(201, result.Code);
        Assert.Equal("Pending", result.Value!.Status);
        Assert.False(result.Value.IsFunded);

        await using var check = _testDb.NewContext();
        var created = await check.Milestones.SingleAsync(m => m.Id == result.Value.Id, TestContext.Current.CancellationToken);
        Assert.Equal(s.ContractId, created.ContractId);
        Assert.Equal(300m, created.Amount);
        Assert.Equal(MilestoneStatus.Pending, created.Status);
        Assert.Equal(2, await check.Milestones.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact(Timeout = 10_000)]
    public async Task Create_ByFreelancer_Returns403_AndCreatesNothing()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100, agreedPrice: 1000);

        var response = await CreateAsync(s.ContractId.ToString(), Valid(), s.FreelancerId, TestContext.Current.CancellationToken);

        Assert.Equal(403, ResultAssert.Unwrap<MilestoneDto>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal(1, await check.Milestones.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact(Timeout = 10_000)]
    public async Task Create_ByStranger_Returns403()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100, agreedPrice: 1000);

        var response = await CreateAsync(s.ContractId.ToString(), Valid(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(403, ResultAssert.Unwrap<MilestoneDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Create_UnknownContract_Returns404()
    {
        var response = await CreateAsync(Guid.NewGuid().ToString(), Valid(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(404, ResultAssert.Unwrap<MilestoneDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Create_InvalidContractId_Returns400()
    {
        var response = await CreateAsync("not-a-guid", Valid(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<MilestoneDto>(response).Code);
    }

    [Theory(Timeout = 10_000)]
    [InlineData(ContractStatus.InDispute)]
    [InlineData(ContractStatus.Completed)]
    [InlineData(ContractStatus.Terminated)]
    public async Task Create_ContractNotActive_Returns409(ContractStatus status)
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100, contractStatus: status, agreedPrice: 1000);

        var response = await CreateAsync(s.ContractId.ToString(), Valid(), s.ClientId, TestContext.Current.CancellationToken);

        Assert.Equal(409, ResultAssert.Unwrap<MilestoneDto>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal(1, await check.Milestones.CountAsync(TestContext.Current.CancellationToken));
    }

    [Theory(Timeout = 10_000)]
    [InlineData("", 100)]
    [InlineData("   ", 100)]
    [InlineData("Этап", 0)]
    [InlineData("Этап", -5)]
    public async Task Create_InvalidBody_Returns400_AndCreatesNothing(string title, decimal amount)
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100, agreedPrice: 1000);

        var response = await CreateAsync(s.ContractId.ToString(), 
            new CreateMilestoneRequest(title, amount, null), s.ClientId, TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<MilestoneDto>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal(1, await check.Milestones.CountAsync(TestContext.Current.CancellationToken));
    }

    // ---- Сумма этапов ----

    [Theory(Timeout = 10_000)]
    [InlineData(900, true)]    // 100 (уже есть) + 900 = 1000 — ровно цена контракта
    [InlineData(901, false)]
    public async Task Create_TotalOfMilestones_CannotExceedAgreedPrice(decimal amount, bool allowed)
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100, agreedPrice: 1000);

        var response = await CreateAsync(s.ContractId.ToString(), 
            new CreateMilestoneRequest("Этап", amount, null), s.ClientId, TestContext.Current.CancellationToken);

        Assert.Equal(allowed ? 201 : 409, ResultAssert.Unwrap<MilestoneDto>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal(allowed ? 2 : 1, await check.Milestones.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact(Timeout = 10_000)]
    public async Task Create_SecondMilestone_CountsAllExistingOnes()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100, agreedPrice: 500);

        Assert.Equal(201, ResultAssert.Unwrap<MilestoneDto>(
            await CreateAsync(s.ContractId.ToString(), 
                new CreateMilestoneRequest("Этап 2", 250, null), s.ClientId, TestContext.Current.CancellationToken)).Code);
        // 100 + 250 уже занято, остаётся 150
        Assert.Equal(409, ResultAssert.Unwrap<MilestoneDto>(
            await CreateAsync(s.ContractId.ToString(), 
                new CreateMilestoneRequest("Этап 3", 200, null), s.ClientId, TestContext.Current.CancellationToken)).Code);
        Assert.Equal(201, ResultAssert.Unwrap<MilestoneDto>(
            await CreateAsync(s.ContractId.ToString(), 
                new CreateMilestoneRequest("Этап 3", 150, null), s.ClientId, TestContext.Current.CancellationToken)).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Create_DueDateInThePast_Returns400()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100, agreedPrice: 1000);

        var response = await CreateAsync(s.ContractId.ToString(),
            new CreateMilestoneRequest("Этап", 100, DateTime.UtcNow.AddDays(-1)), s.ClientId, TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<MilestoneDto>(response).Code);
        await using var check = _testDb.NewContext();
        Assert.Equal(1, await check.Milestones.CountAsync(TestContext.Current.CancellationToken));
    }
}