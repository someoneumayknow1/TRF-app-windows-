using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TRF.NativeClient.Models;

namespace TRF.NativeClient.Api;

public sealed class Bar3ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public Bar3ApiClient(string baseUrl, string? apiKey = null, string? discordSessionCookie = null)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/") };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }

        if (!string.IsNullOrWhiteSpace(discordSessionCookie))
        {
            _httpClient.DefaultRequestHeaders.Add("Cookie", discordSessionCookie);
        }
    }

    public Task<AppData?> GetAppDataAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<AppData>("api/appData", cancellationToken);

    public Task<Config?> GetConfigAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<Config>("api/config", cancellationToken);

    public Task<IReadOnlyList<AnalyticsCampaign>?> GetAnalyticsCampaignsAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<IReadOnlyList<AnalyticsCampaign>>("analytics/campaigns", cancellationToken);

    public Task<JsonElement?> GetAccountAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<JsonElement?>("account", cancellationToken);

    public Task<IReadOnlyList<BotServer>?> GetBotServersAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<IReadOnlyList<BotServer>>("api/bot/servers", cancellationToken);

    public Task<IReadOnlyList<BotCommand>?> GetBotCommandUsageAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<IReadOnlyList<BotCommand>>("api/bot/commands/usage", cancellationToken);

    public Task<bool> SendBotMessageAsync(string content, CancellationToken cancellationToken = default) =>
        PostJsonExpectSuccessAsync("api/bot/send", new { message = content }, cancellationToken);

    public Task<DiscordSession?> GetDiscordSessionAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<DiscordSession>("auth/session", cancellationToken);

    public async Task<IReadOnlyList<Nation>?> GetNationsAsync(CancellationToken cancellationToken = default)
    {
        var endpoints = new[] { "api/nations", "api/nation", "nations", "nation" };
        return await TryGetFromEndpointsAsync<Nation>(endpoints, cancellationToken);
    }

    public async Task<IReadOnlyList<Alliance>?> GetAlliancesAsync(CancellationToken cancellationToken = default)
    {
        var endpoints = new[] { "api/alliances", "api/alliance", "alliances", "alliance" };
        return await TryGetFromEndpointsAsync<Alliance>(endpoints, cancellationToken);
    }

    public async Task<bool> SetConfigAsync(object config, CancellationToken cancellationToken = default)
    {
        var payload = new { config };
        return await PostJsonExpectSuccessAsync("api/setConfig", payload, cancellationToken);
    }

    public async Task<bool> SendMessageAsync(int nationId, string nationName, string leaderName, CancellationToken cancellationToken = default)
    {
        var payload = new { nationID = nationId, nationName, leaderName };
        return await PostJsonExpectSuccessAsync("api/sendMessage", payload, cancellationToken);
    }

    public async Task<bool> SetApplicationStateAsync(bool applicationOn, CancellationToken cancellationToken = default)
    {
        var payload = new { applicationOn };
        return await PostJsonExpectSuccessAsync("api/setApplicationState", payload, cancellationToken);
    }

    public Task<JsonElement?> CreateNewCampaignAsync(string name, CancellationToken cancellationToken = default) =>
        PostJsonAsync<JsonElement?>("analytics/campaigns", new { name }, cancellationToken);

    public async Task<bool> SetBotConfigAsync(object config, CancellationToken cancellationToken = default) =>
        await PostJsonExpectSuccessAsync("api/bot/config", config, cancellationToken);

    public Task<JsonElement?> LoginWithPwApiKeyAsync(string pwApiKey, CancellationToken cancellationToken = default) =>
        PostJsonAsync<JsonElement?>("api/v2/auth/login", new { apiKey = pwApiKey }, cancellationToken);

    public Task<JsonElement?> GetAutomationStateAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<JsonElement?>("api/v2/automation/state", cancellationToken);

    public async Task<bool> SetAutomationStateAsync(bool enabled, CancellationToken cancellationToken = default) =>
        await PostJsonExpectSuccessAsync("api/v2/automation/state", new { enabled }, cancellationToken);

    public async Task<bool> UpsertTemplateAsync(object payload, CancellationToken cancellationToken = default) =>
        await PostJsonExpectSuccessAsync("api/v2/templates", payload, cancellationToken);

    public Task<JsonElement?> GetMyAnalyticsAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<JsonElement?>("api/v2/analytics/me", cancellationToken);

    public Task<JsonElement?> SendActiveUnalliedAsync(bool dryRun, int? minCities, int? maxCities, CancellationToken cancellationToken = default) =>
        PostJsonAsync<JsonElement?>("api/v2/automation/send-active-unallied", new { dryRun, minCities, maxCities }, cancellationToken);

    public Task<JsonElement?> SendActiveUnalliedDiscordAsync(bool dryRun, bool hasDiscord, int? minCities, int? maxCities, CancellationToken cancellationToken = default) =>
        PostJsonAsync<JsonElement?>("api/v2/automation/send-active-unallied-discord", new { dryRun, hasDiscord, minCities, maxCities }, cancellationToken);

    public Task<JsonElement?> SendByNationIdsAsync(bool dryRun, IReadOnlyList<int> nationIds, CancellationToken cancellationToken = default) =>
        PostJsonAsync<JsonElement?>("api/v2/automation/send-by-nation-ids", new { dryRun, nationIds }, cancellationToken);

    public Task<JsonElement?> CheckLatestServerReleaseAsync(CancellationToken cancellationToken = default) =>
        GetAbsoluteJsonAsync<JsonElement?>("https://api.github.com/repos/TheonlyGlaernisch/bar3-server/releases/latest", cancellationToken);

    public string GetDiscordAuthUrl(string? returnTo = null)
    {
        var relative = string.IsNullOrWhiteSpace(returnTo)
            ? "auth/discord"
            : $"auth/discord?returnTo={Uri.EscapeDataString(returnTo)}";

        return new Uri(_httpClient.BaseAddress!, relative).ToString();
    }

    public string GetDiscordLogoutUrl() => new Uri(_httpClient.BaseAddress!, "auth/logout").ToString();

    private async Task<IReadOnlyList<T>?> TryGetFromEndpointsAsync<T>(IEnumerable<string> endpoints, CancellationToken cancellationToken)
    {
        foreach (var endpoint in endpoints)
        {
            var result = await GetJsonAsync<IReadOnlyList<T>>(endpoint, cancellationToken);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private async Task<T?> GetJsonAsync<T>(string relativePath, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(relativePath, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken);
        }
        catch
        {
            return default;
        }
    }

    private async Task<T?> PostJsonAsync<T>(string relativePath, object payload, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(relativePath, payload, _jsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            if (response.Content.Headers.ContentLength == 0)
            {
                return default;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken);
        }
        catch
        {
            return default;
        }
    }

    private async Task<bool> PostJsonExpectSuccessAsync(string relativePath, object payload, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync(relativePath, payload, _jsonOptions, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<T?> GetAbsoluteJsonAsync<T>(string absoluteUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, absoluteUrl);
            request.Headers.UserAgent.ParseAdd("TRF.NativeClient/1.0");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken);
        }
        catch
        {
            return default;
        }
    }

    public void Dispose() => _httpClient.Dispose();
}
