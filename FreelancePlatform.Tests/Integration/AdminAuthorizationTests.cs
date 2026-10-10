namespace FreelancePlatform.Tests.Integration;

public class AdminAuthorizationTests(AppFixture app) : IClassFixture<AppFixture>
{
    private static string ResolveUrl => $"/api/v1/disputes/{Guid.NewGuid()}/resolve";
    private static readonly object ResolveBody = new { Decision = "Client", Resolution = "Решение администратора" };

    private async Task<HttpStatusCode> ResolveAsync(string? token)
    {
        var request = AppFixture.Request(HttpMethod.Post, ResolveUrl, token, ResolveBody);
        var response = await app.Http.SendAsync(request, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    [Fact(Timeout = 180_000)]
    public async Task ResolveDispute_WithoutToken_Returns401() =>
        Assert.Equal(HttpStatusCode.Unauthorized, await ResolveAsync(null));

    [Fact(Timeout = 180_000)]
    public async Task ResolveDispute_ByRegularUser_Returns403()
    {
        var (_, token) = await app.RegisterUserAsync();

        Assert.Equal(HttpStatusCode.Forbidden, await ResolveAsync(token));
    }

    [Fact(Timeout = 180_000)]
    public async Task ResolveDispute_ByAdmin_PassesAuthorization()
    {
        var token = await app.GetAdminTokenAsync();

        // 404, а не 403: роль принята, спора с таким Id просто нет
        Assert.Equal(HttpStatusCode.NotFound, await ResolveAsync(token));
    }

    [Theory(Timeout = 180_000)]
    [InlineData("/api/v1/admin/stats")]
    [InlineData("/api/v1/admin/users")]
    [InlineData("/api/v1/admin/disputes")]
    public async Task AdminReadEndpoints_RejectRegularUser(string url)
    {
        var (_, token) = await app.RegisterUserAsync();
        var request = AppFixture.Request(HttpMethod.Get, url, token);

        var response = await app.Http.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}