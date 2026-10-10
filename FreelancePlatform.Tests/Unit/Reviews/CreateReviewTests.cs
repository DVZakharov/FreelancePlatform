using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Application.DTOs;
using FreelancePlatform.Application.Features.Reviews.CreateReview;
using FreelancePlatform.Domain.Entities;
using FreelancePlatform.Domain.Enums;
using FreelancePlatform.Tests.Helpers;
using Microsoft.AspNetCore.Http;

namespace FreelancePlatform.Tests.Unit.Reviews;

public class CreateReviewTests
{
    private readonly TestDb _testDb = new();

    private async Task<(Guid ClientId, Guid FreelancerId, Guid ContractId)> SeedContractAsync(
        ContractStatus status = ContractStatus.Completed)
    {
        await using var db = _testDb.NewContext();

        var client = Seed.NewUser();
        var freelancer = Seed.NewUser(role: UserRole.Client | UserRole.Freelancer);
        var project = new ProjectEntity
        {
            ClientId = client.Id, Title = "Project", Description = "Description", Status = ProjectStatus.Completed
        };
        var contract = new ContractEntity
        {
            ProjectId = project.Id, ClientId = client.Id, FreelancerId = freelancer.Id,
            AgreedPrice = 100, AgreedDeadline = DateTime.UtcNow, Status = status
        };

        db.AddRange(client, freelancer, project, contract);
        db.FreelancerProfiles.Add(new FreelancerProfileEntity { UserId = freelancer.Id, Bio = "bio", HourlyRate = 10 });

        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return (client.Id, freelancer.Id, contract.Id);
    }

    private Task<IResult> ReviewAsync(Guid contractId, Guid authorId, int rating) =>
        _testDb.RunAsync(db => CreateReviewEndpoint.Handle(
            contractId.ToString(),
            new CreateReviewRequest(rating, "Комментарий"),
            TestHttp.As(authorId),
            new CreateReviewValidator(),
            db, TestContext.Current.CancellationToken));

    [Fact(Timeout = 10_000)]
    public async Task Review_OfFreelancer_RecalculatesAverageRating()
    {
        var (clientId, freelancerId, contractId) = await SeedContractAsync();

        // Старый отзыв по другой работе: 5 звёзд. InMemory не проверяет внешние ключи, поэтому ContractId случайный
        await using (var db = _testDb.NewContext())
        {
            db.Reviews.Add(new ReviewEntity
            {
                ContractId = Guid.NewGuid(), AuthorId = Guid.NewGuid(), TargetId = freelancerId, Rating = 5
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var response = await ReviewAsync(contractId, clientId, rating: 3);

        Assert.Equal(201, ResultAssert.Unwrap<ReviewDto>(response).Code);

        await using var check = _testDb.NewContext();
        var profile = await check.FreelancerProfiles.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(4m, profile.Rating);   // (5 + 3) / 2
    }

    [Fact(Timeout = 10_000)]
    public async Task Review_OfClient_DoesNotTouchFreelancerRating()
    {
        var (_, freelancerId, contractId) = await SeedContractAsync();

        await ReviewAsync(contractId, freelancerId, rating: 1);

        await using var check = _testDb.NewContext();
        Assert.Equal(0m, (await check.FreelancerProfiles.SingleAsync(TestContext.Current.CancellationToken)).Rating);
    }

    [Fact]
    public async Task Review_OnActiveContract_Returns409()
    {
        var (clientId, _, contractId) = await SeedContractAsync(ContractStatus.Active);

        var response = await ReviewAsync(contractId, clientId, 5);

        Assert.Equal(409, ResultAssert.Unwrap<ReviewDto>(response).Code);
    }

    [Fact(Timeout = 10_000)]
    public async Task Review_Twice_BySameAuthor_Returns409_AndKeepsSingleReview()
    {
        var (clientId, _, contractId) = await SeedContractAsync();

        await ReviewAsync(contractId, clientId, 5);
        var second = await ReviewAsync(contractId, clientId, 1);

        Assert.Equal(409, ResultAssert.Unwrap<ReviewDto>(second).Code);

        await using var check = _testDb.NewContext();
        Assert.Equal(1, await check.Reviews.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Review_ByStranger_Returns403()
    {
        var (_, _, contractId) = await SeedContractAsync();

        var response = await ReviewAsync(contractId, Guid.NewGuid(), 5);

        Assert.Equal(403, ResultAssert.Unwrap<ReviewDto>(response).Code);
    }
}