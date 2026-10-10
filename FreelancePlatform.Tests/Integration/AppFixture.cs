using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting;

namespace FreelancePlatform.Tests.Integration;

public sealed class AppFixture : IAsyncLifetime
{
    public const string SetupSecret = "integration-test-secret";
    public const string Password = "Passw0rd!";

    private DistributedApplication _app = null!;
    private Lazy<Task<string>> _adminToken = null!;

    public HttpClient Http { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.FreelancePlatform_AppHost>(ct);

        // Без volume каждый прогон стартует с пустой БД. Иначе тест пойдёт в вашу dev-базу,
        // и bootstrap админа упадёт, если админ там уже есть
        var postgres = appHost.Resources.Single(r => r.Name == "postgres");
        foreach (var mount in postgres.Annotations.OfType<ContainerMountAnnotation>()
                     .Where(m => m.Type == ContainerMountType.Volume).ToList())
            postgres.Annotations.Remove(mount);

        // Секреты для Api. Не уверен в точной сигнатуре EnvironmentCallbackAnnotation в вашей версии Aspire
        var api = appHost.Resources.Single(r => r.Name == "api");
        api.Annotations.Add(new EnvironmentCallbackAnnotation("Admin__SetupSecret", () => SetupSecret));
        api.Annotations.Add(new EnvironmentCallbackAnnotation("Jwt__Secret",
            () => "integration-test-jwt-secret-at-least-32-chars!!"));

        _app = await appHost.BuildAsync(ct);
        await _app.StartAsync(ct);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("api", ct);

        Http = _app.CreateHttpClient("api");
        _adminToken = new Lazy<Task<string>>(CreateAdminAsync);
    }

    public async Task<(string Email, string Token)> RegisterUserAsync()
    {
        var email = $"{Guid.NewGuid():N}@test.local";
        var response = await Http.PostAsJsonAsync("/api/v1/auth/register",
            new { Email = email, Password, FullName = "Test User" },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (email, await ReadTokenAsync(response));
    }

    // Админ создаётся один раз на весь прогон: bootstrap работает, только пока админов нет
    public Task<string> GetAdminTokenAsync() => _adminToken.Value;

    private async Task<string> CreateAdminAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        var (email, _) = await RegisterUserAsync();

        var promote = await Http.PostAsJsonAsync("/api/v1/admin/bootstrap/promote",
            new { Email = email, SetupSecret }, ct);
        promote.EnsureSuccessStatusCode();

        // Роль лежит в токене, поэтому логинимся заново
        var login = await Http.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password }, ct);
        login.EnsureSuccessStatusCode();
        return await ReadTokenAsync(login);
    }

    private static async Task<string> ReadTokenAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var value = Prop(doc.RootElement, "value");
        return Prop(value, "accessToken").GetString()!;
    }

    // Имена свойств в ответе могут быть и PascalCase, и camelCase, поэтому ищем без учёта регистра
    private static JsonElement Prop(JsonElement element, string name) =>
        element.EnumerateObject()
            .First(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Value;

    public static HttpRequestMessage Request(HttpMethod method, string url, string? token = null, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null)
            request.Headers.Authorization = new("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return request;
    }

    public async ValueTask DisposeAsync()
    {
        Http?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }
}