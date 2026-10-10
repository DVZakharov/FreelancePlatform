namespace FreelancePlatform.Tests.Integration;

public sealed record ContractDeal(
    string ClientToken, string FreelancerToken, string FreelancerProfileId, string ProjectId, string ContractId);

public static class Flows
{
    private static readonly HttpMethod Get = HttpMethod.Get;
    private static readonly HttpMethod Post = HttpMethod.Post;

    public static async Task<ApiResult> CallAsync(
        this AppFixture app, HttpMethod method, string url, string? token = null, object? body = null)
    {
        var response = await app.Http.SendAsync(
            AppFixture.Request(method, url, token, body), TestContext.Current.CancellationToken);
        return await ApiResult.FromAsync(response);
    }

    // Запускает count вызовов одновременно: все ждут на одном воротах и стартуют разом
    public static async Task<ApiResult[]> RunConcurrentlyAsync(int count, Func<int, Task<ApiResult>> call)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, count)
            .Select(async i => { await gate.Task; return await call(i); })
            .ToArray();
        gate.SetResult();
        return await Task.WhenAll(tasks);
    }

    public static async Task<string> CreateSkillAsync(this AppFixture app)
    {
        var admin = await app.GetAdminTokenAsync();
        var r = await app.CallAsync(Post, "/api/v1/skills", admin, new { Name = $"Skill {Guid.NewGuid():N}" });
        return r.EnsureSuccess().Str("value", "id");
    }

    public static async Task<string> CreateCategoryAsync(this AppFixture app)
    {
        var admin = await app.GetAdminTokenAsync();
        var r = await app.CallAsync(Post, "/api/v1/categories", admin, new { Name = $"Category {Guid.NewGuid():N}" });
        return r.EnsureSuccess().Str("value", "id");
    }

    public static async Task<(string Token, string ProfileId)> NewFreelancerAsync(this AppFixture app, string skillId)
    {
        var (_, token) = await app.RegisterUserAsync();
        var r = await app.CallAsync(Post, "/api/v1/freelancer-profiles", token,
            new { Bio = new string('a', 60), HourlyRate = 25m, SkillIds = new[] { skillId } });
        return (token, r.EnsureSuccess().Str("value", "id"));
    }

    public static async Task<string> NewOpenProjectAsync(
        this AppFixture app, string clientToken, string categoryId, string skillId)
    {
        var created = await app.CallAsync(Post, "/api/v1/projects", clientToken, new
        {
            CategoryId = categoryId,
            Title = "Сайт на ASP.NET",
            Description = new string('d', 80),
            BudgetType = "Fixed",
            BudgetMin = 100m,
            BudgetMax = 1000m,
            Deadline = DateTime.UtcNow.AddDays(14),
            SkillIds = new[] { skillId }
        });
        var id = created.EnsureSuccess().Str("value", "id");

        (await app.CallAsync(Post, $"/api/v1/projects/{id}/publish", clientToken)).EnsureSuccess();
        return id;
    }

    public static async Task<string> SubmitProposalAsync(
        this AppFixture app, string freelancerToken, string projectId, decimal price)
    {
        var r = await app.CallAsync(Post, $"/api/v1/projects/{projectId}/proposals", freelancerToken,
            new { Price = price, EstimatedDays = 7, CoverLetter = "Готов взяться" });
        return r.EnsureSuccess().Str("value", "id");
    }

    // Всё до принятия отклика: два пользователя, профиль, проект, отклик, контракт
    public static async Task<ContractDeal> ActiveContractAsync(this AppFixture app, decimal price = 500)
    {
        var skillId = await app.CreateSkillAsync();
        var categoryId = await app.CreateCategoryAsync();
        var (_, clientToken) = await app.RegisterUserAsync();
        var (freelancerToken, profileId) = await app.NewFreelancerAsync(skillId);

        var projectId = await app.NewOpenProjectAsync(clientToken, categoryId, skillId);
        var proposalId = await app.SubmitProposalAsync(freelancerToken, projectId, price);

        var accept = await app.CallAsync(Post, $"/api/v1/proposals/{proposalId}/accept", clientToken);
        return new ContractDeal(clientToken, freelancerToken, profileId, projectId, accept.EnsureSuccess().Str("value", "id"));
    }

    public static async Task<string> CreateMilestoneAsync(this AppFixture app, ContractDeal deal, decimal amount)
    {
        var r = await app.CallAsync(Post, $"/api/v1/contracts/{deal.ContractId}/milestones", deal.ClientToken,
            new { Title = "Этап 1", Amount = amount });
        return r.EnsureSuccess().Str("value", "id");
    }

    public static async Task TopUpAsync(this AppFixture app, string token, decimal amount) =>
        (await app.CallAsync(Post, "/api/v1/payments/top-up", token, new { Amount = amount })).EnsureSuccess();

    public static async Task<decimal> BalanceAsync(this AppFixture app, string token) =>
        (await app.CallAsync(Get, "/api/v1/payments/balance", token)).EnsureSuccess().Dec("value", "balance");

    public static async Task<int> TransactionsCountAsync(this AppFixture app, string token, string type)
    {
        var r = await app.CallAsync(Get,
            $"/api/v1/payments/history?Type={type}&PageNumber=1&PageSize=100", token);
        return (int)r.EnsureSuccess().Dec("value", "totalCount");
    }

    // Деньги не создаются и не исчезают: всего пополнено = балансы + заморожено в эскроу
    public static async Task AssertMoneyConservedAsync(this AppFixture app)
    {
        var admin = await app.GetAdminTokenAsync();
        var stats = (await app.CallAsync(Get, "/api/v1/admin/stats", admin)).EnsureSuccess();

        Assert.Equal(
            stats.Dec("value", "totalToppedUp"),
            stats.Dec("value", "totalUsersBalance") + stats.Dec("value", "totalEscrowHeld"));
    }
}