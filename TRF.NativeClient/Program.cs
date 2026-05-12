using System.Diagnostics;
using TRF.NativeClient.Api;
using TRF.NativeClient.Auth;
using TRF.NativeClient.Models;

var serverUrl = Environment.GetEnvironmentVariable("BAR3_SERVER_URL") ?? "http://localhost:8055";
var apiKey = Environment.GetEnvironmentVariable("BAR3_API_KEY");
var discordCookie = Environment.GetEnvironmentVariable("BAR3_DISCORD_COOKIE");

using var client = new Bar3ApiClient(serverUrl, apiKey, discordCookie);

// Fetch the user's Discord session so we can filter tabs by role.
// This is re-fetched after every login attempt so the tab list refreshes.
DiscordSession? currentSession = await client.GetDiscordSessionAsync();

while (true)
{
    var visibleTabs = TabPermissions.VisibleTabs(currentSession);

    Console.Clear();
    Console.WriteLine("TRF Native Client (C#)");
    Console.WriteLine($"Backend: {serverUrl}");

    if (currentSession?.Authenticated == true)
    {
        var roles = currentSession.Roles.Count > 0
            ? string.Join(", ", currentSession.Roles)
            : (currentSession.IsAdmin ? "admin" : "authenticated");
        Console.WriteLine($"Logged in  |  Roles: {roles}");
    }
    else
    {
        Console.WriteLine("Not logged in  –  use 'Discord Auth' to sign in");
    }

    Console.WriteLine();
    Console.WriteLine("Side Tabs:");

    for (var i = 0; i < visibleTabs.Count; i++)
    {
        Console.WriteLine($"  {i + 1}. {visibleTabs[i]}");
    }

    Console.Write("\nSelect tab: ");
    var selected = Console.ReadLine();

    if (!int.TryParse(selected, out var index) || index < 1 || index > visibleTabs.Count)
    {
        continue;
    }

    var tab = visibleTabs[index - 1];
    if (tab == "Exit")
    {
        return;
    }

    Console.Clear();
    Console.WriteLine($"[{tab}]\n");

    // Defence-in-depth: re-check permission even though the tab was already filtered.
    if (!TabPermissions.CanAccess(tab, currentSession))
    {
        Console.WriteLine("Access denied. You do not have the required role for this tab.");
        Console.Write("\nPress Enter to return to side tabs...");
        Console.ReadLine();
        continue;
    }

    switch (tab)
    {
        case "Dashboard":
            var appData = await client.GetAppDataAsync();
            if (appData is null)
            {
                Console.WriteLine("Unable to load dashboard data.");
                break;
            }

            Console.WriteLine($"Application On: {appData.ApplicationOn}");
            Console.WriteLine($"Is Setup:       {appData.IsSetup}");
            Console.WriteLine($"Server Version: {appData.ServerVersion}");
            Console.WriteLine($"API Usage:      {appData.ApiDetails.Used}/{appData.ApiDetails.Max}");
            break;

        case "Configuration":
            var config = await client.GetConfigAsync();
            if (config is null)
            {
                Console.WriteLine("Unable to load configuration.");
                break;
            }

            Console.WriteLine($"Message Subject:    {config.MessageSubject}");
            Console.WriteLine($"Analytics Enabled:  {config.AnalyticsEnabled}");
            break;

        case "Message Creator":
            Console.WriteLine("Endpoint: POST /api/sendMessage");
            Console.WriteLine("Send a sample message? (y/N)");
            var send = Console.ReadLine();
            if (string.Equals(send, "y", StringComparison.OrdinalIgnoreCase))
            {
                var sent = await client.SendMessageAsync(1, "Sample Nation", "Sample Leader");
                Console.WriteLine(sent ? "Send request accepted." : "Send request failed.");
            }
            break;

        case "Analytics":
            var campaigns = await client.GetAnalyticsCampaignsAsync();
            if (campaigns is null)
            {
                Console.WriteLine("Unable to load analytics campaigns.");
                break;
            }

            if (campaigns.Count == 0)
            {
                Console.WriteLine("No analytics campaigns found.");
                break;
            }

            foreach (var campaign in campaigns)
            {
                Console.WriteLine($"- {campaign.Name}");
            }
            break;

        case "Account":
            var account = await client.GetAccountAsync();
            if (account is null)
            {
                Console.WriteLine("Unable to load account data.");
                break;
            }

            Console.WriteLine(account.Value.ToString());
            break;

        case "Automation":
            var automationState = await client.GetAutomationStateAsync();
            if (automationState is null)
            {
                Console.WriteLine("Unable to load automation state.");
                break;
            }

            Console.WriteLine($"Automation state: {automationState.Value}");
            Console.WriteLine();
            Console.WriteLine("Toggle automation? (y/N)");
            var toggle = Console.ReadLine();
            if (string.Equals(toggle, "y", StringComparison.OrdinalIgnoreCase))
            {
                // Try v2 first, fall back to legacy setApplicationState.
                bool currentEnabled = false;
                if (automationState.Value.TryGetProperty("enabled", out var enabledEl))
                {
                    currentEnabled = enabledEl.GetBoolean();
                }

                var ok = await client.SetAutomationStateAsync(!currentEnabled);
                Console.WriteLine(ok ? $"Automation set to {!currentEnabled}." : "Toggle failed.");
            }
            break;

        case "Nation":
            var nations = await client.GetNationsAsync();
            if (nations is null)
            {
                Console.WriteLine("Unable to load nation list from configured backend endpoints.");
                break;
            }

            foreach (var nation in nations.Take(15))
            {
                Console.WriteLine($"#{nation.NationId} {nation.NationName} ({nation.Leader}) - {nation.Alliance} | Cities: {nation.Cities} | Score: {nation.Score:0.##}");
            }
            break;

        case "Alliance":
            var alliances = await client.GetAlliancesAsync();
            if (alliances is null)
            {
                Console.WriteLine("Unable to load alliance list from configured backend endpoints.");
                break;
            }

            foreach (var alliance in alliances.Take(15))
            {
                Console.WriteLine($"#{alliance.AllianceId} {alliance.Name} | Members: {alliance.Members} | Score: {alliance.Score:0.##}");
            }
            break;

        case "Bot Panel":
            var botStatus = await client.GetBotStatusAsync();
            if (botStatus is null)
            {
                Console.WriteLine("Unable to load bot status.");
                break;
            }

            Console.WriteLine($"Bot status: {botStatus.Value}");
            break;

        case "Discord Auth":
            // Re-fetch session so information is current.
            currentSession = await client.GetDiscordSessionAsync();

            if (currentSession is null)
            {
                Console.WriteLine("Session check failed – server may be unreachable.");
            }
            else
            {
                Console.WriteLine($"Authenticated: {currentSession.Authenticated}");
                Console.WriteLine($"Is Admin:      {currentSession.IsAdmin}");
                Console.WriteLine($"Roles:         {(currentSession.Roles.Count == 0 ? "(none)" : string.Join(", ", currentSession.Roles))}");

                Console.WriteLine();
                Console.WriteLine("Visible tabs based on your roles:");
                foreach (var t in TabPermissions.VisibleTabs(currentSession))
                {
                    Console.WriteLine($"  - {t}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Open Discord login URL in browser? (y/N)");
            var open = Console.ReadLine();
            if (string.Equals(open, "y", StringComparison.OrdinalIgnoreCase))
            {
                var authUrl = client.GetDiscordAuthUrl("/dashboard");
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
                Console.WriteLine($"Opened: {authUrl}");
                Console.WriteLine("After signing in, press Enter to refresh your session...");
                Console.ReadLine();
                // Refresh session after the user completes OAuth in the browser.
                currentSession = await client.GetDiscordSessionAsync();
                Console.WriteLine(currentSession?.Authenticated == true
                    ? $"Signed in. Roles: {string.Join(", ", currentSession.Roles)}"
                    : "Session not yet authenticated.");
            }
            break;

        case "Endpoint Coverage":
            Console.WriteLine("Implemented endpoint coverage:");
            Console.WriteLine("- GET  /api/appData");
            Console.WriteLine("- GET  /api/config");
            Console.WriteLine("- POST /api/setConfig");
            Console.WriteLine("- POST /api/sendMessage");
            Console.WriteLine("- POST /api/setApplicationState");
            Console.WriteLine("- GET  /analytics/campaigns");
            Console.WriteLine("- POST /analytics/campaigns");
            Console.WriteLine("- GET  /api/nations | /api/nation | /nations | /nation");
            Console.WriteLine("- GET  /api/alliances | /api/alliance | /alliances | /alliance");
            Console.WriteLine("- GET  /auth/session");
            Console.WriteLine("- Browser URL /auth/discord and /auth/logout");
            Console.WriteLine("- GET  /account");
            Console.WriteLine("- GET  /api/bot/status");
            Console.WriteLine("- POST /api/bot/config");
            Console.WriteLine("- POST /api/v2/auth/login");
            Console.WriteLine("- GET  /api/v2/automation/state");
            Console.WriteLine("- POST /api/v2/automation/state");
            Console.WriteLine("- POST /api/v2/templates");
            Console.WriteLine("- GET  /api/v2/analytics/me");
            Console.WriteLine("- POST /api/v2/automation/send-active-unallied");
            Console.WriteLine("- POST /api/v2/automation/send-active-unallied-discord");
            Console.WriteLine("- POST /api/v2/automation/send-by-nation-ids");
            Console.WriteLine("- GET  GitHub releases (check-for-updates)");
            break;
    }

    Console.Write("\nPress Enter to return to side tabs...");
    Console.ReadLine();
}
