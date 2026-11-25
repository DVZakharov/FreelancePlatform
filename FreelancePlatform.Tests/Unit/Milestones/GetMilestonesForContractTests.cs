using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Milestones.GetMilestonesForContract;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Milestones;

public class GetMilestonesForContractTests
{
    private readonly TestDb _testDb = new();

    private Task<Microsoft.AspNetCore.Http.IResult> GetAsync(string contractId, Guid userId, CancellationToken ct) =>
        _testDb.RunAsync(db => GetMilestonesForContractEndpoint.Handle(
            contractId, new GetMilestonesForContractValidator(), TestHttp.As(userId), db, ct));

    [Fact(Timeout = 10_000)]
    public async Task Get_ReturnsOnlyThisContractsMilestones_ToBothParticipants()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100);
        await Seed.MilestoneAsync(_testDb, 0, 100); // этап чужого контракта
        await using (var db = _testDb.NewContext())
        {
            db.Milestones.Add(new MilestoneEntity { ContractId = s.ContractId, Title = "Второй", Amount = 50 });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        foreach (var userId in new[] { s.ClientId, s.FreelancerId })
        {
            var result = ResultAssert.Unwrap<List<MilestoneDto>>(await GetAsync(s.ContractId.ToString(), userId, TestContext.Current.CancellationToken));
            Assert.Equal(200, result.Code);
            Assert.Equal(2, result.Value!.Count);
        }
    }

    [Fact(Timeout = 10_000)]
    public async Task Get_Stranger_Returns403()
    {
        var s = await Seed.MilestoneAsync(_testDb, 0, 100);

        var response = await GetAsync(s.ContractId.ToString(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(403, ResultAssert.Unwrap<List<MilestoneDto>>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Get_Unknown_Returns404()
    {
        var response = await GetAsync(Guid.NewGuid().ToString(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(404, ResultAssert.Unwrap<List<MilestoneDto>>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Get_InvalidId_Returns400()
    {
        var response = await GetAsync("not-a-guid", Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<List<MilestoneDto>>(response).Code);
    }
}