using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Proposals.SubmitProposal;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Tests.Unit.Proposals;

public class SubmitProposalTests
{
    private readonly TestDb _testDb = new();

    private Task<IResult> SubmitAsync(Guid projectId, Guid userId) =>
        _testDb.RunAsync(db => SubmitProposalEndpoint.Handle(
            projectId.ToString(),
            new SubmitProposalRequest(500, 7, "Готов взяться"),
            TestHttp.As(userId),
            new SubmitProposalValidator(),
            db, TestContext.Current.CancellationToken));

    [Fact(Timeout = 10_000)]
    public async Task Submit_ByFreelancerWithProfile_CreatesPendingProposal()
    {
        var s = await Seed.OpenProjectAsync(_testDb);

        var response = await SubmitAsync(s.ProjectId, s.FreelancerId);

        Assert.Equal(201, ResultAssert.Unwrap<ProposalDto>(response).Code);

        await using var check = _testDb.NewContext();
        var proposal = await check.Proposals.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(ProposalStatus.Pending, proposal.Status);
        Assert.Equal(500, proposal.Price);
    }

    [Fact]
    public async Task Submit_WithoutFreelancerProfile_Returns400()
    {
        var s = await Seed.OpenProjectAsync(_testDb, freelancerHasProfile: false);

        var response = await SubmitAsync(s.ProjectId, s.FreelancerId);

        Assert.Equal(400, ResultAssert.Unwrap<ProposalDto>(response).Code);
    }

    [Fact]
    public async Task Submit_ToOwnProject_Returns400()
    {
        var s = await Seed.OpenProjectAsync(_testDb);

        var response = await SubmitAsync(s.ProjectId, s.ClientId);

        Assert.Equal(400, ResultAssert.Unwrap<ProposalDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Submit_Twice_Returns409_AndKeepsSingleProposal()
    {
        var s = await Seed.OpenProjectAsync(_testDb);

        await SubmitAsync(s.ProjectId, s.FreelancerId);
        var second = await SubmitAsync(s.ProjectId, s.FreelancerId);

        Assert.Equal(409, ResultAssert.Unwrap<ProposalDto>(second).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(1, await check.Proposals.CountAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(ProjectStatus.Draft)]
    [InlineData(ProjectStatus.InProgress)]
    [InlineData(ProjectStatus.Cancelled)]
    public async Task Submit_ToNonOpenProject_Returns409(ProjectStatus status)
    {
        var s = await Seed.OpenProjectAsync(_testDb, status: status);

        var response = await SubmitAsync(s.ProjectId, s.FreelancerId);

        Assert.Equal(409, ResultAssert.Unwrap<ProposalDto>(response).Code);
    }
}