namespace FreelancePlatform.Tests.Integration;

[Collection("Integration")]
public class ConcurrencyTests(AppFixture app)
{
    private static readonly HttpMethod Get = HttpMethod.Get;
    private static readonly HttpMethod Post = HttpMethod.Post;

    private static void AssertNoServerErrors(IEnumerable<ApiResult> results)
    {
        var failed = results.Where(r => r.Code >= 500).ToList();
        Assert.True(failed.Count == 0, "Сервер вернул 5xx: " + string.Join(" | ", failed.Select(r => r.Raw)));
    }

    [Fact(Timeout = 300_000)]
    public async Task Accept_OnDifferentProposalsAtOnce_CreatesExactlyOneContract()
    {
        var skillId = await app.CreateSkillAsync();
        var categoryId = await app.CreateCategoryAsync();
        var freelancers = new List<string>();
        for (var i = 0; i < 3; i++)
            freelancers.Add((await app.NewFreelancerAsync(skillId)).Token);

        // Несколько раундов: одна попытка гонку может и не поймать
        for (var round = 0; round < 5; round++)
        {
            var (_, clientToken) = await app.RegisterUserAsync();
            var projectId = await app.NewOpenProjectAsync(clientToken, categoryId, skillId);
            var proposalIds = new List<string>();
            foreach (var token in freelancers)
                proposalIds.Add(await app.SubmitProposalAsync(token, projectId, 500));

            var results = await Flows.RunConcurrentlyAsync(proposalIds.Count, i =>
                app.CallAsync(Post, $"/api/v1/proposals/{proposalIds[i]}/accept", clientToken));

            Assert.Equal(1, results.Count(r => r.IsSuccess));

            var contracts = (await app.CallAsync(Get, "/api/v1/contracts/me", clientToken)).EnsureSuccess();
            Assert.Single(contracts.List("value", "items"));

            var proposals = (await app.CallAsync(Get, $"/api/v1/projects/{projectId}/proposals", clientToken)).EnsureSuccess();
            Assert.Equal(1, proposals.Pluck(["value"], "status").Count(s => s == "Accepted"));

            AssertNoServerErrors(results);
        }
    }

    [Fact(Timeout = 300_000)]
    public async Task SubmitProposal_FiveTimesAtOnce_KeepsSingleProposal()
    {
        var skillId = await app.CreateSkillAsync();
        var categoryId = await app.CreateCategoryAsync();
        var (_, clientToken) = await app.RegisterUserAsync();
        var (freelancerToken, _) = await app.NewFreelancerAsync(skillId);
        var projectId = await app.NewOpenProjectAsync(clientToken, categoryId, skillId);

        var results = await Flows.RunConcurrentlyAsync(5, _ =>
            app.CallAsync(Post, $"/api/v1/projects/{projectId}/proposals", freelancerToken,
                new { Price = 500m, EstimatedDays = 7, CoverLetter = "Готов взяться" }));

        Assert.Equal(1, results.Count(r => r.IsSuccess));

        var proposals = (await app.CallAsync(Get, $"/api/v1/projects/{projectId}/proposals", clientToken)).EnsureSuccess();
        Assert.Single(proposals.List("value"));

        AssertNoServerErrors(results);
    }

    [Fact(Timeout = 300_000)]
    public async Task Fund_FiveTimesAtOnce_FreezesMoneyOnce()
    {
        var deal = await app.ActiveContractAsync(price: 300);
        await app.TopUpAsync(deal.ClientToken, 1000);
        var milestoneId = await app.CreateMilestoneAsync(deal, 300);

        var results = await Flows.RunConcurrentlyAsync(5, _ =>
            app.CallAsync(Post, $"/api/v1/milestones/{milestoneId}/fund", deal.ClientToken));

        Assert.Equal(1, results.Count(r => r.IsSuccess));
        Assert.Equal(700m, await app.BalanceAsync(deal.ClientToken));
        Assert.Equal(1, await app.TransactionsCountAsync(deal.ClientToken, "Freeze"));
        await app.AssertMoneyConservedAsync();

        AssertNoServerErrors(results);
    }

    [Fact(Timeout = 300_000)]
    public async Task Approve_FiveTimesAtOnce_PaysFreelancerOnce()
    {
        var deal = await app.ActiveContractAsync(price: 300);
        await app.TopUpAsync(deal.ClientToken, 1000);
        var milestoneId = await app.CreateMilestoneAsync(deal, 300);
        (await app.CallAsync(Post, $"/api/v1/milestones/{milestoneId}/fund", deal.ClientToken)).EnsureSuccess();
        (await app.CallAsync(Post, $"/api/v1/milestones/{milestoneId}/submit", deal.FreelancerToken)).EnsureSuccess();

        var results = await Flows.RunConcurrentlyAsync(5, _ =>
            app.CallAsync(Post, $"/api/v1/milestones/{milestoneId}/approve", deal.ClientToken));

        Assert.Equal(1, results.Count(r => r.IsSuccess));
        Assert.Equal(1, await app.TransactionsCountAsync(deal.FreelancerToken, "Release"));
        Assert.Equal(300m, await app.BalanceAsync(deal.FreelancerToken));
        await app.AssertMoneyConservedAsync();

        AssertNoServerErrors(results);
    }

    [Fact(Timeout = 300_000)]
    public async Task TopUp_FiveTimesAtOnce_BalanceMatchesSuccessfulRequests()
    {
        var (_, token) = await app.RegisterUserAsync();

        var results = await Flows.RunConcurrentlyAsync(5, _ =>
            app.CallAsync(Post, "/api/v1/payments/top-up", token, new { Amount = 100m }));

        var succeeded = results.Count(r => r.IsSuccess);
        Assert.True(succeeded >= 1);

        // Каждый успешный запрос обязан увеличить баланс, иначе деньги потеряны
        Assert.Equal(100m * succeeded, await app.BalanceAsync(token));
        Assert.Equal(succeeded, await app.TransactionsCountAsync(token, "TopUp"));
        await app.AssertMoneyConservedAsync();

        AssertNoServerErrors(results);
    }
}