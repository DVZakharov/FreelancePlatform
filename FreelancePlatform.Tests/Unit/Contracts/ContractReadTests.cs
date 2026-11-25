using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Contracts.GetContractById;
using FreelancePlatform.Application.Features.Contracts.GetMyContracts;
using FreelancePlatform.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Tests.Unit.Contracts;

public class ContractReadTests
{
    private readonly TestDb _testDb = new();

    private Task<Microsoft.AspNetCore.Http.IResult> GetByIdAsync(string id, Guid userId, CancellationToken ct) =>
        _testDb.RunAsync(db => GetContractByIdEndpoint.Handle(
            id, new GetContractByIdValidator(), TestHttp.As(userId), db, ct));

    [Fact(Timeout = 10_000)]
    public async Task GetById_Client_Returns200WithDetails()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100);

        var response = await GetByIdAsync(s.ContractId.ToString(), s.ClientId, TestContext.Current.CancellationToken);

        var result = ResultAssert.Unwrap<ContractDto>(response);
        Assert.Equal(200, result.Code);
        Assert.Equal(s.ContractId, result.Value!.Id);
        Assert.Equal(s.ClientId, result.Value.ClientId);
        Assert.Equal(s.FreelancerId, result.Value.FreelancerId);
        Assert.Equal("Test User", result.Value.ClientName);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetById_Freelancer_Returns200()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100);

        var response = await GetByIdAsync(s.ContractId.ToString(), s.FreelancerId, TestContext.Current.CancellationToken);

        Assert.Equal(200, ResultAssert.Unwrap<ContractDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetById_Stranger_Returns403()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100);

        var response = await GetByIdAsync(s.ContractId.ToString(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(403, ResultAssert.Unwrap<ContractDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetById_Unknown_Returns404()
    {
        var response = await GetByIdAsync(Guid.NewGuid().ToString(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(404, ResultAssert.Unwrap<ContractDto>(response).Code);
    }

    [Theory(Timeout = 10_000)]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task GetById_InvalidId_Returns400(string id)
    {
        var response = await GetByIdAsync(id, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<ContractDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetMy_ReturnsContractsInBothRoles_WithMyRole_AndNoForeignOnes()
    {
        var first = await Seed.MilestoneAsync(_testDb, 0, 100);   // first.ClientId — заказчик
        var second = await Seed.MilestoneAsync(_testDb, 0, 100);
        await Seed.MilestoneAsync(_testDb, 0, 100);               // чужой контракт

        // first.ClientId становится фрилансером во втором контракте
        await using (var db = _testDb.NewContext())
        {
            var contract = await db.Contracts.SingleAsync(c => c.Id == second.ContractId, TestContext.Current.CancellationToken);
            contract.FreelancerId = first.ClientId;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await _testDb.RunAsync(db => GetMyContractsEndpoint.Handle(TestHttp.As(first.ClientId), db, TestContext.Current.CancellationToken));

        var items = ResultAssert.Unwrap<GetMyContractsResponse>(response).Value!.Items;
        Assert.Equal(2, items.Count);
        Assert.Equal(new[] { "Client", "Freelancer" }, items.Select(i => i.MyRole).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal("Client", items.Single(i => i.Contract.Id == first.ContractId).MyRole);
        Assert.Equal("Freelancer", items.Single(i => i.Contract.Id == second.ContractId).MyRole);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetMy_UserWithoutContracts_ReturnsEmptyList()
    {
        var response = await _testDb.RunAsync(db => GetMyContractsEndpoint.Handle(TestHttp.As(Guid.NewGuid()), db, TestContext.Current.CancellationToken));

        var result = ResultAssert.Unwrap<GetMyContractsResponse>(response);
        Assert.Equal(200, result.Code);
        Assert.Empty(result.Value!.Items);
    }
}
