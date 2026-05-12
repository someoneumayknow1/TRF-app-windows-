using TRF.NativeClient.Models;

namespace TRF.NativeClient.Auth;

/// <summary>
/// Maps side-tab names to the roles required to access them.
/// Role names are matched case-insensitively against the strings in
/// <see cref="DiscordSession.Roles"/> returned by /auth/session.
///
/// Role hierarchy used by the TRF application:
///   admin  – full access (Bot Panel, Endpoint Coverage, and all user/member tabs).
///   user   – Dashboard, Configuration, Message Creator, Analytics, Account, Automation.
///   member – Nation and Alliance tabs.
///
/// A single user may hold more than one role (e.g. ["user", "member"]).
/// <see cref="DiscordSession.IsAdmin"/> is treated as an implicit "admin" role.
/// </summary>
public static class TabPermissions
{
    // Role name constants (checked case-insensitively).
    public const string RoleAdmin  = "admin";
    public const string RoleUser   = "user";
    public const string RoleMember = "member";

    // For every tab, the set of roles that grant access.
    // An empty set means the tab is always visible (no auth required).
    private static readonly Dictionary<string, HashSet<string>> _requiredRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        // Always visible – used for login / logout flow.
        ["Discord Auth"]      = [],
        ["Exit"]              = [],

        // Requires the "user" role (or admin).
        ["Dashboard"]         = [RoleUser, RoleAdmin],
        ["Configuration"]     = [RoleUser, RoleAdmin],
        ["Message Creator"]   = [RoleUser, RoleAdmin],
        ["Analytics"]         = [RoleUser, RoleAdmin],
        ["Account"]           = [RoleUser, RoleAdmin],
        ["Automation"]        = [RoleUser, RoleAdmin],

        // Requires the "member" role (or admin).
        ["Nation"]            = [RoleMember, RoleAdmin],
        ["Alliance"]          = [RoleMember, RoleAdmin],

        // Requires the "admin" role.
        ["Bot Panel"]         = [RoleAdmin],
        ["Endpoint Coverage"] = [RoleAdmin],
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
        if (session is null || !session.Authenticated)
        {
            return false;
        }

        // Admins can access everything.
        if (session.IsAdmin)
        {
            return true;
        }

        // Check whether any of the user's roles satisfy the tab's requirements.
        var userRoles = session.Roles
            .Select(r => r.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return required.Overlaps(userRoles);
    }

    /// <summary>
    /// Returns the ordered subset of <see cref="AllTabs"/> the user may see.
    /// </summary>
    public static IReadOnlyList<string> VisibleTabs(DiscordSession? session) =>
        AllTabs.Where(t => CanAccess(t, session)).ToList();
}
