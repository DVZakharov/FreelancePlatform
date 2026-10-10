using System.Text.Json;

namespace FreelancePlatform.Tests.Integration;

public sealed class ApiResult
{
    private readonly JsonElement _root;

    private ApiResult(HttpStatusCode status, string raw, JsonElement root)
    {
        Status = status; Raw = raw; _root = root;
    }

    public HttpStatusCode Status { get; }
    public string Raw { get; }
    public int Code => (int)Status;
    public bool IsSuccess => Code is >= 200 and < 300;

    public static async Task<ApiResult> FromAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        JsonElement root = default;
        if (!string.IsNullOrWhiteSpace(text))
        {
            using var doc = JsonDocument.Parse(text);
            root = doc.RootElement.Clone();
        }
        return new ApiResult(response.StatusCode, text, root);
    }

    public ApiResult EnsureSuccess() =>
        IsSuccess ? this : throw new InvalidOperationException($"Ожидался 2xx, пришло {Code}: {Raw}");

    // Путь по вложенным свойствам без учёта регистра: Find("value", "id")
    public JsonElement? Find(params string[] path)
    {
        var current = _root;
        foreach (var name in path)
        {
            if (current.ValueKind != JsonValueKind.Object) return null;
            JsonElement? next = null;
            foreach (var p in current.EnumerateObject())
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) { next = p.Value; break; }
            if (next is null) return null;
            current = next.Value;
        }
        return current;
    }

    public string Str(params string[] path) =>
        Find(path)?.GetString() ?? throw Missing(path);

    public decimal Dec(params string[] path) =>
        Find(path)?.GetDecimal() ?? throw Missing(path);

    public List<JsonElement> List(params string[] path) =>
        Find(path) is { ValueKind: JsonValueKind.Array } array ? [.. array.EnumerateArray()] : throw Missing(path);

    // Значения одного поля у всех элементов массива
    public List<string?> Pluck(string[] listPath, string field) =>
        [.. List(listPath).Select(item => item.EnumerateObject()
            .FirstOrDefault(p => string.Equals(p.Name, field, StringComparison.OrdinalIgnoreCase)).Value.GetString())];

    private InvalidOperationException Missing(string[] path) =>
        new($"В ответе {Code} нет '{string.Join('.', path)}': {Raw}");
}