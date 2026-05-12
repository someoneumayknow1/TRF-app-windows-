using System.Net.Http.Headers;
using System.Text.Json;
using TRF.NativeClient.Models;

namespace TRF.NativeClient.Api;

public sealed class Bar3ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public Bar3ApiClient(string baseUrl, string? apiKey = null)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/") };
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        }
    }

    public Task<AppData?> GetAppDataAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<AppData>("api/appData", cancellationToken);

    public Task<Config?> GetConfigAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<Config>("api/config", cancellationToken);

    public Task<IReadOnlyList<AnalyticsCampaign>?> GetAnalyticsCampaignsAsync(CancellationToken cancellationToken = default) =>
        GetJsonAsync<IReadOnlyList<AnalyticsCampaign>>("analytics/campaigns", cancellationToken);

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

    public void Dispose() => _httpClient.Dispose();
}
