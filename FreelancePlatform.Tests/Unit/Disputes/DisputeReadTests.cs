using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Disputes.GetDisputeById;
using FreelancePlatform.Application.Features.Disputes.GetDisputesForContract;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;

namespace FreelancePlatform.Tests.Unit.Disputes;

public class DisputeReadTests
{
    private readonly TestDb _testDb = new();

    private async Task<Guid> SeedUserAsync()
    {
        await using var db = _testDb.NewContext();
        var user = Seed.NewUser();
        db.Users.Add(user);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return user.Id;
    }

    private Task<Microsoft.AspNetCore.Http.IResult> GetByIdAsync(string id, Guid userId, CancellationToken ct) =>
        _testDb.RunAsync(db => GetDisputeByIdEndpoint.Handle(
            id, new GetDisputeByIdValidator(), TestHttp.As(userId), db, ct));

    private Task<Microsoft.AspNetCore.Http.IResult> ForContractAsync(string contractId, Guid userId, CancellationToken ct) =>
        _testDb.RunAsync(db => GetDisputesForContractEndpoint.Handle(
            contractId, new GetDisputesForContractValidator(), TestHttp.As(userId), db, ct));

    // ---- GetDisputeById: участники и администратор ----

    [Fact(Timeout = 10_000)]
    public async Task GetById_Client_Freelancer_AndAdmin_CanRead()
    {
        var s = await Seed.DisputeAsync(_testDb, 0, 100);

        foreach (var userId in new[] { s.ClientId, s.FreelancerId, s.AdminId })
        {
            var result = ResultAssert.Unwrap<DisputeDto>(await GetByIdAsync(s.DisputeId.ToString(), userId, TestContext.Current.CancellationToken));
            Assert.Equal(200, result.Code);
            Assert.Equal(s.DisputeId, result.Value!.Id);
        }
    }

    [Fact(Timeout = 10_000)]
    public async Task GetById_RegularStranger_Returns403()
    {
        var s = await Seed.DisputeAsync(_testDb, 0, 100);
        var stranger = await SeedUserAsync();

        var response = await GetByIdAsync(s.DisputeId.ToString(), stranger, TestContext.Current.CancellationToken);

        Assert.Equal(403, ResultAssert.Unwrap<DisputeDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task GetById_Unknown_Returns404()
    {
        var response = await GetByIdAsync(Guid.NewGuid().ToString(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(404, ResultAssert.Unwrap<DisputeDto>(response).Code);
    }

    [Theory(Timeout = 10_000)]
    [InlineData("not-a-guid")]
    [InlineData("")]
    public async Task GetById_InvalidId_Returns400(string id)
    {
        var response = await GetByIdAsync(id, Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<DisputeDto>(response).Code);
    }

    // ---- GetDisputesForContract: только участники (админ здесь не предусмотрен) ----

    [Fact(Timeout = 10_000)]
    public async Task ForContract_ReturnsAllIncludingResolved_ToParticipants()
    {
        var s = await Seed.DisputeAsync(_testDb, 0, 100);
        await using (var db = _testDb.NewContext())
        {
            db.Disputes.Add(new DisputeEntity
            {
                ContractId = s.ContractId, OpenedById = s.FreelancerId, Reason = "Старый спор",
                Status = DisputeStatus.ResolvedForClient, Resolution = "Решён", ResolvedById = s.AdminId
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        foreach (var userId in new[] { s.ClientId, s.FreelancerId })
        {
            var result = ResultAssert.Unwrap<List<DisputeDto>>(await ForContractAsync(s.ContractId.ToString(), userId, TestContext.Current.CancellationToken));
            Assert.Equal(200, result.Code);
            Assert.Equal(2, result.Value!.Count);
        }
    }

    [Fact(Timeout = 10_000)]
    public async Task ForContract_DoesNotMixDisputesOfOtherContracts()
    {
        var s = await Seed.DisputeAsync(_testDb, 0, 100);
        await Seed.DisputeAsync(_testDb, 0, 100);

        var result = ResultAssert.Unwrap<List<DisputeDto>>(await ForContractAsync(s.ContractId.ToString(), s.ClientId, TestContext.Current.CancellationToken));

        Assert.Single(result.Value!);
        Assert.Equal(s.DisputeId, result.Value![0].Id);
    }

    [Fact(Timeout = 10_000)]
    public async Task ForContract_Stranger_Returns403()
    {
        var s = await Seed.DisputeAsync(_testDb, 0, 100);

        var response = await ForContractAsync(s.ContractId.ToString(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(403, ResultAssert.Unwrap<List<DisputeDto>>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task ForContract_Unknown_Returns404()
    {
        var response = await ForContractAsync(Guid.NewGuid().ToString(), Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(404, ResultAssert.Unwrap<List<DisputeDto>>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task ForContract_InvalidId_Returns400()
    {
        var response = await ForContractAsync("not-a-guid", Guid.NewGuid(), TestContext.Current.CancellationToken);

        Assert.Equal(400, ResultAssert.Unwrap<List<DisputeDto>>(response).Code);
    }
}
