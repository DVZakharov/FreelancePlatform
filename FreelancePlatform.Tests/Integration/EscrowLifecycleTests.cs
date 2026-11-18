namespace FreelancePlatform.Tests.Integration;

/// <summary>
/// Сквозные сценарии с деньгами через настоящий HTTP и Postgres: эскроу, споры, отзывы.
/// После каждого сценария проверяем сохранение денег: пополнено = балансы + заморожено.
/// </summary>
[Collection("Integration")]
public class EscrowLifecycleTests(AppFixture app)
{
    private static readonly HttpMethod Get = HttpMethod.Get;
    private static readonly HttpMethod Post = HttpMethod.Post;

    private const string DisputeReason = "Работа выполнена не по техническому заданию";

    private Task<ApiResult> PostAsync(string url, string token, object? body = null) =>
        app.CallAsync(Post, url, token, body);

    private async Task<string> MilestoneStatusAsync(ContractDeal deal, string milestoneId)
    {
        var list = await app.CallAsync(Get, $"/api/v1/contracts/{deal.ContractId}/milestones", deal.ClientToken);
        var item = list.EnsureSuccess().List("value")
            .Single(m => m.GetProperty("id").GetString() == milestoneId);
        return item.GetProperty("status").GetString()!;
    }

    private async Task<string> ContractStatusAsync(ContractDeal deal) =>
        (await app.CallAsync(Get, $"/api/v1/contracts/{deal.ContractId}", deal.ClientToken))
            .EnsureSuccess().Str("value", "status");

    // Контракт на 300, клиент пополнил 1000, этап на 300 профинансирован и сдан
    private async Task<(ContractDeal Deal, string MilestoneId)> SubmittedMilestoneAsync()
    {
        var deal = await app.ActiveContractAsync(price: 300);
        await app.TopUpAsync(deal.ClientToken, 1000);
        var milestoneId = await app.CreateMilestoneAsync(deal, 300);
        (await PostAsync($"/api/v1/milestones/{milestoneId}/fund", deal.ClientToken)).EnsureSuccess();
        (await PostAsync($"/api/v1/milestones/{milestoneId}/submit", deal.FreelancerToken)).EnsureSuccess();
        return (deal, milestoneId);
    }

    [Fact(Timeout = 300_000)]
    public async Task HappyPath_FundSubmitRejectResubmitApproveCompleteReview()
    {
        var deal = await app.ActiveContractAsync(price: 300);
        await app.TopUpAsync(deal.ClientToken, 1000);
        var milestoneId = await app.CreateMilestoneAsync(deal, 300);

        (await PostAsync($"/api/v1/milestones/{milestoneId}/fund", deal.ClientToken)).EnsureSuccess();
        Assert.Equal(700m, await app.BalanceAsync(deal.ClientToken));
        Assert.Equal("Funded", await MilestoneStatusAsync(deal, milestoneId));

        (await PostAsync($"/api/v1/milestones/{milestoneId}/submit", deal.FreelancerToken)).EnsureSuccess();
        Assert.Equal("Submitted", await MilestoneStatusAsync(deal, milestoneId));

        // Отклонение: деньги остаются в эскроу, никто ничего не получает и не теряет
        (await PostAsync($"/api/v1/milestones/{milestoneId}/reject", deal.ClientToken,
            new { Reason = "Нужны правки по верстке" })).EnsureSuccess();
        Assert.Equal("Funded", await MilestoneStatusAsync(deal, milestoneId));
        Assert.Equal(700m, await app.BalanceAsync(deal.ClientToken));
        Assert.Equal(0m, await app.BalanceAsync(deal.FreelancerToken));
        await app.AssertMoneyConservedAsync();

        (await PostAsync($"/api/v1/milestones/{milestoneId}/submit", deal.FreelancerToken)).EnsureSuccess();
        (await PostAsync($"/api/v1/milestones/{milestoneId}/approve", deal.ClientToken)).EnsureSuccess();

        Assert.Equal("Completed", await MilestoneStatusAsync(deal, milestoneId));
        Assert.Equal(700m, await app.BalanceAsync(deal.ClientToken));
        Assert.Equal(300m, await app.BalanceAsync(deal.FreelancerToken));
        Assert.Equal(1, await app.TransactionsCountAsync(deal.FreelancerToken, "Release"));

        (await PostAsync($"/api/v1/contracts/{deal.ContractId}/complete", deal.ClientToken)).EnsureSuccess();
        Assert.Equal("Completed", await ContractStatusAsync(deal));

        // Отзывы: клиент оценивает фрилансера, и средний рейтинг профиля пересчитывается
        (await PostAsync($"/api/v1/contracts/{deal.ContractId}/reviews", deal.ClientToken,
            new { Rating = 4, Comment = "Хорошая работа" })).EnsureSuccess();
        var profile = await app.CallAsync(Get, $"/api/v1/freelancer-profiles/{deal.FreelancerProfileId}");
        Assert.Equal(4m, profile.EnsureSuccess().Dec("value", "rating"));

        // Повторный отзыв от той же стороны запрещён
        var duplicate = await PostAsync($"/api/v1/contracts/{deal.ContractId}/reviews", deal.ClientToken,
            new { Rating = 1, Comment = "Передумал" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.Status);

        await app.AssertMoneyConservedAsync();
    }

    [Fact(Timeout = 300_000)]
    public async Task Fund_WithoutEnoughBalance_IsRejected_AndNothingMoves()
    {
        var deal = await app.ActiveContractAsync(price: 300);
        await app.TopUpAsync(deal.ClientToken, 100);
        var milestoneId = await app.CreateMilestoneAsync(deal, 300);

        var result = await PostAsync($"/api/v1/milestones/{milestoneId}/fund", deal.ClientToken);

        Assert.False(result.IsSuccess, result.Raw);
        Assert.True(result.Code < 500, result.Raw);
        Assert.Equal(100m, await app.BalanceAsync(deal.ClientToken));
        Assert.Equal("Pending", await MilestoneStatusAsync(deal, milestoneId));
        Assert.Equal(0, await app.TransactionsCountAsync(deal.ClientToken, "Freeze"));
        await app.AssertMoneyConservedAsync();
    }

    [Fact(Timeout = 300_000)]
    public async Task Approve_ByFreelancer_IsForbidden_AndMoneyStaysInEscrow()
    {
        var (deal, milestoneId) = await SubmittedMilestoneAsync();

        var result = await PostAsync($"/api/v1/milestones/{milestoneId}/approve", deal.FreelancerToken);

        Assert.Equal(HttpStatusCode.Forbidden, result.Status);
        Assert.Equal(0m, await app.BalanceAsync(deal.FreelancerToken));
        Assert.Equal("Submitted", await MilestoneStatusAsync(deal, milestoneId));
    }

    [Fact(Timeout = 300_000)]
    public async Task Dispute_BlocksMilestoneActions_UntilAdminResolves_InFavorOfClient()
    {
        var (deal, milestoneId) = await SubmittedMilestoneAsync();
        var admin = await app.GetAdminTokenAsync();

        var opened = await PostAsync($"/api/v1/contracts/{deal.ContractId}/disputes", deal.FreelancerToken,
            new { Reason = DisputeReason });
        Assert.Equal(HttpStatusCode.Created, opened.Status);
        var disputeId = opened.Str("value", "id");
        Assert.Equal("InDispute", await ContractStatusAsync(deal));

        // Пока спор открыт, принять этап нельзя
        var approve = await PostAsync($"/api/v1/milestones/{milestoneId}/approve", deal.ClientToken);
        Assert.Equal(HttpStatusCode.Conflict, approve.Status);
        Assert.Equal(0m, await app.BalanceAsync(deal.FreelancerToken));

        (await PostAsync($"/api/v1/disputes/{disputeId}/resolve", admin,
            new { Decision = "Client", Resolution = "Работа не соответствует ТЗ" })).EnsureSuccess();

        Assert.Equal(1000m, await app.BalanceAsync(deal.ClientToken));
        Assert.Equal(0m, await app.BalanceAsync(deal.FreelancerToken));
        Assert.Equal(1, await app.TransactionsCountAsync(deal.ClientToken, "Refund"));
        Assert.Equal("Active", await ContractStatusAsync(deal));
        await app.AssertMoneyConservedAsync();

        // Повторное решение по тому же спору невозможно: деньги второй раз не вернуть
        var again = await PostAsync($"/api/v1/disputes/{disputeId}/resolve", admin,
            new { Decision = "Client", Resolution = "Повтор" });
        Assert.Equal(HttpStatusCode.Conflict, again.Status);
        Assert.Equal(1000m, await app.BalanceAsync(deal.ClientToken));
    }

    [Fact(Timeout = 300_000)]
    public async Task Dispute_ResolvedInFavorOfFreelancer_PaysFreelancer()
    {
        var (deal, _) = await SubmittedMilestoneAsync();
        var admin = await app.GetAdminTokenAsync();

        var opened = await PostAsync($"/api/v1/contracts/{deal.ContractId}/disputes", deal.ClientToken,
            new { Reason = DisputeReason });
        var disputeId = opened.EnsureSuccess().Str("value", "id");

        (await PostAsync($"/api/v1/disputes/{disputeId}/resolve", admin,
            new { Decision = "Freelancer", Resolution = "Работа выполнена полностью" })).EnsureSuccess();

        Assert.Equal(700m, await app.BalanceAsync(deal.ClientToken));
        Assert.Equal(300m, await app.BalanceAsync(deal.FreelancerToken));
        Assert.Equal(1, await app.TransactionsCountAsync(deal.FreelancerToken, "Release"));
        await app.AssertMoneyConservedAsync();
    }

    [Fact(Timeout = 300_000)]
    public async Task Dispute_StrangerCannotOpenIt()
    {
        var (deal, _) = await SubmittedMilestoneAsync();
        var (_, strangerToken) = await app.RegisterUserAsync();

        var result = await PostAsync($"/api/v1/contracts/{deal.ContractId}/disputes", strangerToken,
            new { Reason = DisputeReason });

        Assert.Equal(HttpStatusCode.Forbidden, result.Status);
        Assert.Equal("Active", await ContractStatusAsync(deal));
    }
}
