namespace FreelancePlatform.Tests.Integration;

/// <summary>
/// Справочники (категории и навыки): чтение публичное, изменение только для Admin.
/// Проверяем весь HTTP-конвейер: JWT, роли, хендлер и реальный Postgres.
/// </summary>
[Collection("Integration")]
public class CatalogAuthorizationTests(AppFixture app)
{
    private static readonly HttpMethod Get = HttpMethod.Get;
    private static readonly HttpMethod Post = HttpMethod.Post;
    private static readonly HttpMethod Put = HttpMethod.Put;
    private static readonly HttpMethod Delete = HttpMethod.Delete;

    private static string UniqueName(string prefix) => $"{prefix} {Guid.NewGuid():N}"[..30];

    // Все изменяющие эндпоинты справочников. Id случайный: авторизация срабатывает раньше хендлера
    public static TheoryData<string, string, bool> MutatingEndpoints() => new()
    {
        { "POST", "/api/v1/categories", true },
        { "PUT", "/api/v1/categories/{id}", true },
        { "DELETE", "/api/v1/categories/{id}", false },
        { "POST", "/api/v1/skills", true },
        { "PUT", "/api/v1/skills/{id}", true },
        { "DELETE", "/api/v1/skills/{id}", false },
    };

    private async Task<ApiResult> CallAsync(string method, string url, string? token, bool withBody) =>
        await app.CallAsync(new HttpMethod(method), url.Replace("{id}", Guid.NewGuid().ToString()), token,
            withBody ? new { Name = UniqueName("Name") } : null);

    [Theory(Timeout = 180_000)]
    [MemberData(nameof(MutatingEndpoints))]
    public async Task Mutation_WithoutToken_Returns401(string method, string url, bool withBody)
    {
        var result = await CallAsync(method, url, null, withBody);

        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
    }

    [Theory(Timeout = 180_000)]
    [MemberData(nameof(MutatingEndpoints))]
    public async Task Mutation_ByRegularUser_Returns403(string method, string url, bool withBody)
    {
        var (_, token) = await app.RegisterUserAsync();

        var result = await CallAsync(method, url, token, withBody);

        Assert.Equal(HttpStatusCode.Forbidden, result.Status);
    }

    [Theory(Timeout = 180_000)]
    [MemberData(nameof(MutatingEndpoints))]
    public async Task Mutation_ByFreelancerWhoIsNotAdmin_Returns403(string method, string url, bool withBody)
    {
        var skillId = await app.CreateSkillAsync();
        var (token, _) = await app.NewFreelancerAsync(skillId);

        var result = await CallAsync(method, url, token, withBody);

        Assert.Equal(HttpStatusCode.Forbidden, result.Status);
    }

    [Theory(Timeout = 180_000)]
    [InlineData("/api/v1/categories")]
    [InlineData("/api/v1/skills")]
    public async Task List_IsPublic(string url)
    {
        var result = await app.CallAsync(Get, url);

        Assert.Equal(HttpStatusCode.OK, result.Status);
    }

    [Theory(Timeout = 180_000)]
    [InlineData("categories")]
    [InlineData("skills")]
    public async Task Admin_FullLifecycle_CreateUpdateGetDelete(string resource)
    {
        var admin = await app.GetAdminTokenAsync();
        var baseUrl = $"/api/v1/{resource}";

        var created = await app.CallAsync(Post, baseUrl, admin, new { Name = UniqueName("Create") });
        Assert.Equal(HttpStatusCode.Created, created.Status);
        var id = created.Str("value", "id");

        var newName = UniqueName("Renamed");
        var updated = await app.CallAsync(Put, $"{baseUrl}/{id}", admin, new { Name = newName });
        Assert.Equal(HttpStatusCode.OK, updated.Status);
        Assert.Equal(newName, updated.Str("value", "name"));

        // GET публичный, токен не нужен
        var fetched = await app.CallAsync(Get, $"{baseUrl}/{id}");
        Assert.Equal(newName, fetched.EnsureSuccess().Str("value", "name"));

        Assert.Equal(HttpStatusCode.OK, (await app.CallAsync(Delete, $"{baseUrl}/{id}", admin)).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await app.CallAsync(Get, $"{baseUrl}/{id}")).Status);
    }

    [Theory(Timeout = 180_000)]
    [InlineData("categories")]
    [InlineData("skills")]
    public async Task Admin_DuplicateNameInDifferentCase_Returns409(string resource)
    {
        var admin = await app.GetAdminTokenAsync();
        var name = UniqueName("Dup");
        (await app.CallAsync(Post, $"/api/v1/{resource}", admin, new { Name = name })).EnsureSuccess();

        var duplicate = await app.CallAsync(Post, $"/api/v1/{resource}", admin, new { Name = name.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.Status);
    }

    // Регрессия: у связи Project -> Category стоит Restrict, раньше удаление давало 500 из Postgres
    [Fact(Timeout = 180_000)]
    public async Task Admin_DeleteCategoryWithProjects_Returns409_AndKeepsIt()
    {
        var admin = await app.GetAdminTokenAsync();
        var skillId = await app.CreateSkillAsync();
        var categoryId = await app.CreateCategoryAsync();
        var (_, clientToken) = await app.RegisterUserAsync();
        await app.NewOpenProjectAsync(clientToken, categoryId, skillId);

        var result = await app.CallAsync(Delete, $"/api/v1/categories/{categoryId}", admin);

        Assert.Equal(HttpStatusCode.Conflict, result.Status);
        Assert.Equal(HttpStatusCode.OK, (await app.CallAsync(Get, $"/api/v1/categories/{categoryId}")).Status);
    }

    // Проба: навык привязан к проекту и профилю через many-to-many. Что бы ни решили по бизнес-логике
    // (каскад или запрет), клиент не должен получать 500 из-за нарушения внешнего ключа
    [Fact(Timeout = 180_000)]
    public async Task Admin_DeleteSkillInUse_DoesNotFailWithServerError()
    {
        var admin = await app.GetAdminTokenAsync();
        var skillId = await app.CreateSkillAsync();
        var categoryId = await app.CreateCategoryAsync();
        var (_, clientToken) = await app.RegisterUserAsync();
        await app.NewOpenProjectAsync(clientToken, categoryId, skillId);
        await app.NewFreelancerAsync(skillId);

        var result = await app.CallAsync(Delete, $"/api/v1/skills/{skillId}", admin);

        Assert.True(result.Code < 500, $"Удаление используемого навыка вернуло {result.Code}: {result.Raw}");
    }
}
