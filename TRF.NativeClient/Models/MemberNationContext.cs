using System.Text.Json.Serialization;

namespace TRF.NativeClient.Models;

public sealed class MemberNationContext
{
    [JsonPropertyName("registered")]
    public bool Registered { get; set; }

    [JsonPropertyName("nation")]
    public MemberNation? Nation { get; set; }

    [JsonPropertyName("alliance")]
    public MemberAlliance? Alliance { get; set; }
}

public sealed class MemberNation
{
    [JsonPropertyName("nationId")]
    public int NationId { get; set; }

    [JsonPropertyName("nationName")]
    public string NationName { get; set; } = string.Empty;

    [JsonPropertyName("leaderName")]
    public string LeaderName { get; set; } = string.Empty;

    [JsonPropertyName("allianceName")]
    public string AllianceName { get; set; } = string.Empty;

    [JsonPropertyName("numCities")]
    public int NumCities { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }
}

public sealed class MemberAlliance
{
    [JsonPropertyName("allianceId")]
    public int AllianceId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("numMembers")]
    public int NumMembers { get; set; }

    [JsonPropertyName("score")]
    public double Score { get; set; }
}
