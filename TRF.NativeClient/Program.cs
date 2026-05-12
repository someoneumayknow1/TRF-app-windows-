using TRF.NativeClient.Api;

var serverUrl = Environment.GetEnvironmentVariable("BAR3_SERVER_URL") ?? "http://localhost:8055";
var apiKey = Environment.GetEnvironmentVariable("BAR3_API_KEY");

using var client = new Bar3ApiClient(serverUrl, apiKey);

var tabs = new[]
{
    "Dashboard",
    "Configuration",
    "Message Creator",
    "Analytics",
    "Nation",
    "Alliance",
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
            Console.WriteLine("Message creation remains server-driven via /api/sendMessage endpoint.");
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
    }

    Console.Write("\nPress Enter to return to side tabs...");
    Console.ReadLine();
}
