using TRF.NativeClient.Models;

namespace TRF.NativeClient.Auth;

/// <summary>
/// Maps side-tab names to the roles required to access them.
/// Role names are matched case-insensitively against the strings in
/// <see cref="DiscordSession.GetGrantedRoles"/> derived from /auth/session.
///
/// Role hierarchy used by the TRF application:
///   bar3_server  – full access (Bot Panel, Endpoint Coverage, and all bar3_client/member_guild tabs).
///   bar3_client  – Dashboard, Configuration, Message Creator, Analytics, Account, Automation.
///   member_guild – Nation and Alliance tabs.
///
/// A single user may hold more than one role.
/// <see cref="DiscordSession.IsAdmin"/>/<see cref="DiscordSession.AdminAuthenticated"/>
/// are treated as implicit admin access for backwards compatibility.
/// </summary>
public static class TabPermissions
{
    // Role name constants (checked case-insensitively).
    public const string RoleAdmin = "admin";
    public const string RoleBar3Server = "bar3_server";
    public const string RoleBar3Client = "bar3_client";
    public const string RoleMemberGuild = "member_guild";

    // For every tab, the set of roles that grant access.
    // An empty set means the tab is always visible (no auth required).
    private static readonly Dictionary<string, HashSet<string>> _requiredRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        // Always visible – used for login / logout flow.
        ["Discord Auth"]      = [],
        ["Exit"]              = [],

        // Requires the "bar3_client" role (or admin).
        ["Dashboard"]         = [RoleBar3Client, RoleBar3Server, RoleAdmin],
        ["Configuration"]     = [RoleBar3Client, RoleBar3Server, RoleAdmin],
        ["Message Creator"]   = [RoleBar3Client, RoleBar3Server, RoleAdmin],
        ["Analytics"]         = [RoleBar3Client, RoleBar3Server, RoleAdmin],
        ["Account"]           = [RoleBar3Client, RoleBar3Server, RoleAdmin],
        ["Automation"]        = [RoleBar3Client, RoleBar3Server, RoleAdmin],

        // Requires the "member_guild" role (or admin).
        ["Nation"]            = [RoleMemberGuild, RoleBar3Server, RoleAdmin],
        ["Alliance"]          = [RoleMemberGuild, RoleBar3Server, RoleAdmin],

        // Requires "bar3_server" (or admin compatibility role).
        ["Bot Panel"]         = [RoleBar3Server, RoleAdmin],
        ["Endpoint Coverage"] = [RoleBar3Server, RoleAdmin],
    };

    /// <summary>All known tabs in display order.</summary>
    public static readonly IReadOnlyList<string> AllTabs = [
        "Discord Auth",
        "Dashboard",
        "Configuration",
        "Message Creator",
        "Analytics",
        "Account",
        "Automation",
        "Nation",
        "Alliance",
        "Bot Panel",
        "Endpoint Coverage",
        "Exit",
    ];

    /// <summary>
    /// Returns true when <paramref name="session"/> grants access to <paramref name="tab"/>.
    /// </summary>
    public static bool CanAccess(string tab, DiscordSession? session)
    {
        if (!_requiredRoles.TryGetValue(tab, out var required))
        {
            // Unknown tab – deny by default.
            return false;
        }

        // Tab has no role requirement – always accessible.
        if (required.Count == 0)
        {
            return true;
        }

        // Must be authenticated.
        if (session is null || !session.IsAuthenticated)
        {
            return false;
        }

        // Admins can access everything.
        if (session.HasAdminAccess)
        {
            return true;
        }

        // Check whether any of the user's roles satisfy the tab's requirements.
        var userRoles = session.GetGrantedRoles();

        return required.Overlaps(userRoles);
    }

    /// <summary>
    /// Returns the ordered subset of <see cref="AllTabs"/> the user may see.
    /// </summary>
    public static IReadOnlyList<string> VisibleTabs(DiscordSession? session) =>
        AllTabs.Where(t => CanAccess(t, session)).ToList();
}
