using System.Text.Json.Serialization;

namespace TRF.NativeClient.Models;

public sealed class BotCommand
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("usageCount")]
    public int UsageCount { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
