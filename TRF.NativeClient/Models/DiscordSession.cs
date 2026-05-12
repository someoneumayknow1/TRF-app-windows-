using System.Text.Json.Serialization;

namespace TRF.NativeClient.Models;

public sealed class DiscordSession
{
    [JsonPropertyName("authenticated")]
    public bool Authenticated { get; set; }

    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = [];
}
