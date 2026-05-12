using System.Text.Json.Serialization;

namespace TRF.NativeClient.Models;

public sealed class Alliance
{
    [JsonPropertyName("alliance_id")]
    public int AllianceId { get; set; }

    [JsonPropertyName("alliance")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("members")]
    public int Members { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }
}
