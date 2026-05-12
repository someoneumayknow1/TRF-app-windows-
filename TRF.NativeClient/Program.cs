using System.Diagnostics;
using TRF.NativeClient.Api;

var serverUrl = Environment.GetEnvironmentVariable("BAR3_SERVER_URL") ?? "http://localhost:8055";
var apiKey = Environment.GetEnvironmentVariable("BAR3_API_KEY");
var discordCookie = Environment.GetEnvironmentVariable("BAR3_DISCORD_COOKIE");

using var client = new Bar3ApiClient(serverUrl, apiKey, discordCookie);

var tabs = new[]
{
    "Dashboard",
    "Configuration",
    "Message Creator",
    "Analytics",
    "Nation",
    "Alliance",
    "Discord Auth",
    "Endpoint Coverage",
    "Exit"
};

while (true)
{
    Console.Clear();
    Console.WriteLine("TRF Native Client (C#)");
    Console.WriteLine($"Backend: {serverUrl}");
    Console.WriteLine();
    Console.WriteLine("Side Tabs:");

    for (var i = 0; i < tabs.Length; i++)
    {
        Console.WriteLine($"  {i + 1}. {tabs[i]}");
    }

    Console.Write("\nSelect tab: ");
    var selected = Console.ReadLine();

    if (!int.TryParse(selected, out var index) || index < 1 || index > tabs.Length)
    {
        continue;
    }

    var tab = tabs[index - 1];
    if (tab == "Exit")
    {
        return;
    }

    Console.Clear();
    Console.WriteLine($"[{tab}]\n");

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
            Console.WriteLine($"Is Setup: {appData.IsSetup}");
            Console.WriteLine($"Server Version: {appData.ServerVersion}");
            Console.WriteLine($"API: {appData.ApiDetails.Used}/{appData.ApiDetails.Max}");
            break;

        case "Configuration":
            var config = await client.GetConfigAsync();
            if (config is null)
            {
                Console.WriteLine("Unable to load configuration.");
                break;
            }

            Console.WriteLine($"Message Subject: {config.MessageSubject}");
            Console.WriteLine($"Analytics Enabled: {config.AnalyticsEnabled}");
            break;

        case "Message Creator":
            Console.WriteLine("Message creator endpoint: POST /api/sendMessage");
            Console.WriteLine("Set sample nation details and send? (y/N)");
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

        case "Discord Auth":
            var session = await client.GetDiscordSessionAsync();
            if (session is null)
            {
                Console.WriteLine("Session check failed.");
            }
            else
            {
                Console.WriteLine($"Authenticated: {session.Authenticated}");
                Console.WriteLine($"Is Admin: {session.IsAdmin}");
                Console.WriteLine($"Roles: {(session.Roles.Count == 0 ? "(none)" : string.Join(", ", session.Roles))}");
            }

            Console.WriteLine();
            Console.WriteLine("Open Discord login URL in browser? (y/N)");
            var open = Console.ReadLine();
            if (string.Equals(open, "y", StringComparison.OrdinalIgnoreCase))
            {
                var authUrl = client.GetDiscordAuthUrl("/dashboard");
                Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });
                Console.WriteLine($"Opened: {authUrl}");
            }
            break;

        case "Endpoint Coverage":
            Console.WriteLine("Implemented endpoint coverage:");
            Console.WriteLine("- GET /api/appData");
            Console.WriteLine("- GET /api/config");
            Console.WriteLine("- POST /api/setConfig");
            Console.WriteLine("- POST /api/sendMessage");
            Console.WriteLine("- POST /api/setApplicationState");
            Console.WriteLine("- GET /analytics/campaigns");
            Console.WriteLine("- POST /analytics/campaigns");
            Console.WriteLine("- GET /api/nations | /api/nation | /nations | /nation");
            Console.WriteLine("- GET /api/alliances | /api/alliance | /alliances | /alliance");
            Console.WriteLine("- GET /auth/session");
            Console.WriteLine("- Browser URL /auth/discord and /auth/logout");
            Console.WriteLine("- GET /account");
            Console.WriteLine("- GET /api/bot/status");
            Console.WriteLine("- POST /api/bot/config");
            Console.WriteLine("- POST /api/v2/auth/login");
            Console.WriteLine("- GET /api/v2/automation/state");
            Console.WriteLine("- POST /api/v2/automation/state");
            Console.WriteLine("- POST /api/v2/templates");
            Console.WriteLine("- GET /api/v2/analytics/me");
            Console.WriteLine("- POST /api/v2/automation/send-active-unallied");
            Console.WriteLine("- POST /api/v2/automation/send-active-unallied-discord");
            Console.WriteLine("- POST /api/v2/automation/send-by-nation-ids");
            Console.WriteLine("- GET GitHub release endpoint used by check-for-updates");
            break;
    }

    Console.Write("\nPress Enter to return to side tabs...");
    Console.ReadLine();
}
