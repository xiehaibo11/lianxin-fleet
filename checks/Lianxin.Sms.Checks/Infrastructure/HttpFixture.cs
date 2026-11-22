using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using Lianxin.Sms.Api.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;

namespace Lianxin.Sms.Checks.Infrastructure;

internal sealed class HttpFixture : IAsyncDisposable
{
    public const string Password = "Test-only-lianxin-2026";
    public const string DemoPassword = "Demo-lianxin-2026";
    private readonly WebApplication app;
    private readonly List<HttpClient> clients = [];
    private readonly string directory;
    public Uri Origin
    {
        get;
    }
    public string DatabasePath => Path.Combine(directory, "sms.sqlite");
    private HttpFixture(WebApplication app, string directory)
    {
        this.app = app;
        this.directory = directory;
        Origin = new Uri(app.Urls.Single());
    }
    public static async Task<HttpFixture> Create(bool demo)
    {
        var directory = Path.Combine(Path.GetTempPath(), "lianxin-sms-http-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var app = SmsApplication.Build([], new SmsOptions(Path.Combine(directory, "sms.sqlite"), demo, Password));
        app.Urls.Clear();
        app.Urls.Add("http://127.0.0.1:0");
        try
        {
            await app.StartAsync();
            return new(app, directory);
        }
        catch { await app.DisposeAsync(); SqliteConnection.ClearAllPools(); Directory.Delete(directory, true); throw; }
    }
    public ApiClient Client(string? bearer = null, string? origin = null, string? cookie = null, string? forwardedFor = null)
    {
        var http = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer(), AllowAutoRedirect = false })
        {
            BaseAddress = Origin,
            Timeout = TimeSpan.FromSeconds(20)
        };
        if (bearer is not null)
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        if (origin is not null)
            http.DefaultRequestHeaders.Add("Origin", origin);
        if (cookie is not null)
            http.DefaultRequestHeaders.Add("Cookie", cookie);
        if (forwardedFor is not null)
            http.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);
        clients.Add(http);
        return new(http);
    }
    public async Task<ApiClient> Login(string email, bool demo = false)
    {
        var client = Client();
        await client.Call("/login", new
        {
            email,
            password = demo ? DemoPassword : Password
        });
        return client;
    }
    public async ValueTask DisposeAsync()
    {
        foreach (var client in clients)
            client.Dispose();
        await app.StopAsync();
        await app.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(directory, true);
    }
}

internal sealed class ApiClient(HttpClient http)
{
    public async Task Raw(string path, HttpMethod method, string content, string mediaType, int expected)
    {
        using var request = new HttpRequestMessage(method, "/api" + path)
        {
            Content = new StringContent(content, System.Text.Encoding.UTF8, mediaType)
        };
        using var response = await http.SendAsync(request);
        Assert.Equal(expected, (int)response.StatusCode);
    }
    public async Task<JsonElement> Call(string path, object? body = null, int expected = 200)
    {
        using var response = body is null ? await http.GetAsync("/api" + path)
            : await http.PostAsJsonAsync("/api" + path, body);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True((int)response.StatusCode == expected, $"{path}: expected HTTP {expected}, got {(int)response.StatusCode}: {content}");
        using var json = JsonDocument.Parse(content);
        return json.RootElement.Clone();
    }
}
