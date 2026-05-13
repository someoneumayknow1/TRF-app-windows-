using System.Text.Json.Serialization;

namespace TRF.NativeClient.Models;

public sealed class BotServer
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("memberCount")]
    public int MemberCount { get; set; }
}
