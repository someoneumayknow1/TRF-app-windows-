using System.Text.Json.Serialization;

namespace TRF.NativeClient.Models;

public sealed class Nation
{
    [JsonPropertyName("nation_id")]
    public int NationId { get; set; }

    [JsonPropertyName("nation")]
    public string NationName { get; set; } = string.Empty;

    [JsonPropertyName("leader")]
    public string Leader { get; set; } = string.Empty;

    [JsonPropertyName("alliance_id")]
    public int AllianceId { get; set; }

    [JsonPropertyName("alliance")]
    public string Alliance { get; set; } = string.Empty;

    [JsonPropertyName("cities")]
    public int Cities { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }
}
