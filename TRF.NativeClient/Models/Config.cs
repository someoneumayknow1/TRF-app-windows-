using System.Text.Json.Serialization;

namespace TRF.NativeClient.Models;

public sealed class Config
{
    [JsonPropertyName("apiKey")]
    public string? ApiKey { get; set; }

    [JsonPropertyName("messageSubject")]
    public string? MessageSubject { get; set; }

    [JsonPropertyName("messageHTML")]
    public string? MessageHtml { get; set; }

    [JsonPropertyName("analyticsEnabled")]
    public bool AnalyticsEnabled { get; set; }
}
