using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Disputes.OpenDispute;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Disputes;

public class OpenDisputeTests
{
    private readonly TestDb _testDb = new();
    private const string Reason = "Работа не соответствует ТЗ, прошу разобраться";

    [Theory(Timeout = 10_000)]
    [InlineData(true)]    // клиент
    [InlineData(false)]   // фрилансер
    public async Task Open_ByEitherParty_CreatesDispute_AndFreezesContract(bool byClient)
    {
        var s = await Seed.MilestoneAsync(_testDb, 700, 300, MilestoneStatus.Funded, withEscrow: true);
        var actor = byClient ? s.ClientId : s.FreelancerId;

        var response = await _testDb.RunAsync(db => OpenDisputeEndpoint.Handle(
            s.ContractId.ToString(), new OpenDisputeRequest(Reason),
            TestHttp.As(actor), new OpenDisputeValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(201, ResultAssert.Unwrap<DisputeDto>(response).Code);

        await using var check = _testDb.NewContext();
        var dispute = await check.Disputes.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(DisputeStatus.Open, dispute.Status);
        Assert.Equal(actor, dispute.OpenedById);
        Assert.Equal(ContractStatus.InDispute, (await check.Contracts.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Fact(Timeout = 10_000)]
    public async Task Open_ByStranger_Returns403_AndOpensNothing()
    {
        var s = await Seed.MilestoneAsync(_testDb, 700, 300);

        var response = await _testDb.RunAsync(db => OpenDisputeEndpoint.Handle(
            s.ContractId.ToString(), new OpenDisputeRequest(Reason),
            TestHttp.As(Guid.NewGuid()), new OpenDisputeValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(403, ResultAssert.Unwrap<DisputeDto>(response).Code);

        await using var check = _testDb.NewContext();
        Assert.Empty(check.Disputes);
        Assert.Equal(ContractStatus.Active, (await check.Contracts.SingleAsync(TestContext.Current.CancellationToken)).Status);
    }

    [Fact(Timeout = 10_000)]
    public async Task Open_WhenContractAlreadyInDispute_Returns409_AndKeepsSingleDispute()
    {
        var s = await Seed.MilestoneAsync(_testDb, 700, 300, contractStatus: ContractStatus.Active);

        var first = await _testDb.RunAsync(db => OpenDisputeEndpoint.Handle(
            s.ContractId.ToString(), new OpenDisputeRequest( Reason),
            TestHttp.As(s.ClientId), new OpenDisputeValidator(), db, TestContext.Current.CancellationToken));
        var second = await _testDb.RunAsync(db => OpenDisputeEndpoint.Handle(
            s.ContractId.ToString(), new OpenDisputeRequest(Reason),
            TestHttp.As(s.FreelancerId), new OpenDisputeValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(201, ResultAssert.Unwrap<DisputeDto>(first).Code);
        Assert.Equal(409, ResultAssert.Unwrap<DisputeDto>(second).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(1, await check.Disputes.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact(Timeout = 10_000)]
    public async Task Open_WithShortReason_Returns400()
    {
        var s = await Seed.MilestoneAsync(_testDb, 700, 300);

        var response = await _testDb.RunAsync(db => OpenDisputeEndpoint.Handle(
            s.ContractId.ToString(), new OpenDisputeRequest("коротко"),
            TestHttp.As(s.ClientId), new OpenDisputeValidator(), db, TestContext.Current.CancellationToken));

        Assert.Equal(400, ResultAssert.Unwrap<DisputeDto>(response).Code);
    }
}