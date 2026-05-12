using System.Text.Json.Serialization;

namespace TRF.NativeClient.Models;

public sealed class AppData
{
    [JsonPropertyName("applicationOn")]
    public bool ApplicationOn { get; set; }

    [JsonPropertyName("isSetup")]
    public bool IsSetup { get; set; }

    [JsonPropertyName("sentMessages")]
    public List<object> SentMessages { get; set; } = [];

    [JsonPropertyName("apiDetails")]
    public ApiDetails ApiDetails { get; set; } = new();

    [JsonPropertyName("serverVersion")]
    public string ServerVersion { get; set; } = string.Empty;
}

public sealed class ApiDetails
{
    [JsonPropertyName("used")]
    public int Used { get; set; }

    [JsonPropertyName("max")]
    public int Max { get; set; }
}
