using System.Text.Json.Serialization;

namespace TRF.NativeClient.Models;

public sealed class DiscordSession
{
    [JsonPropertyName("authenticated")]
    public bool Authenticated { get; set; }

    [JsonPropertyName("discordAuthenticated")]
    public bool DiscordAuthenticated { get; set; }

    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("adminAuthenticated")]
    public bool AdminAuthenticated { get; set; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; } = [];

    [JsonPropertyName("discordRoles")]
    public DiscordRoles? RoleFlags { get; set; }

    [JsonIgnore]
    public bool IsAuthenticated => Authenticated || DiscordAuthenticated;

    [JsonIgnore]
    public bool HasAdminAccess => IsAdmin || AdminAuthenticated;

    public HashSet<string> GetGrantedRoles()
    {
        var granted = Roles
            .Select(r => r.Trim())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (RoleFlags?.Verified == true)
        {
            granted.Add("verified");
        }

        if (RoleFlags?.Bar3Client == true)
        {
            granted.Add("bar3_client");
        }

        if (RoleFlags?.Bar3Server == true)
        {
            granted.Add("bar3_server");
        }

        if (RoleFlags?.MemberGuild == true)
        {
            granted.Add("member_guild");
        }

        if (HasAdminAccess)
        {
            granted.Add("admin");
        }

        return granted;
    }

    public IReadOnlyList<string> GetDisplayRoles()
    {
        if (!IsAuthenticated)
        {
            return [];
        }

        var granted = GetGrantedRoles();
        return granted.Count == 0
            ? ["authenticated"]
            : granted.OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToList();
    }
}

public sealed class DiscordRoles
{
    [JsonPropertyName("verified")]
    public bool Verified { get; set; }

    [JsonPropertyName("bar3_client")]
    public bool Bar3Client { get; set; }

    [JsonPropertyName("bar3_server")]
    public bool Bar3Server { get; set; }

    [JsonPropertyName("member_guild")]
    public bool MemberGuild { get; set; }
}
