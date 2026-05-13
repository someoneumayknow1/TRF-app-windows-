using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TRF.NativeClient.Api;
using TRF.NativeClient.Auth;
using TRF.NativeClient.Models;

namespace TRF.NativeClient;

public partial class MainWindow : Window
{
    private Bar3ApiClient _client;
    private DiscordSession? _session;
    private string _currentTab = "";
    private readonly string _serverUrl;

    public MainWindow()
    {
        InitializeComponent();
        _serverUrl = Environment.GetEnvironmentVariable("BAR3_SERVER_URL") ?? "https://your-actual-server.com";
        var apiKey = Environment.GetEnvironmentVariable("BAR3_API_KEY");
        var cookie = Environment.GetEnvironmentVariable("BAR3_DISCORD_COOKIE");
        _client = new Bar3ApiClient(_serverUrl, apiKey, cookie);
        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        await CheckForUpdatesAsync();
        _session = await _client.GetDiscordSessionAsync();
        RefreshNav();
        await NavigateToAsync("Discord Auth");
    }

    private async Task CheckForUpdatesAsync()
    {
        const string currentVersion = "v0.1.1";
        try
        {
            var release = await _client.CheckLatestServerReleaseAsync();
            if (release is null) return;
            var latestTag = release.Value.GetProperty("tag_name").GetString();
            if (latestTag == currentVersion) return;

            var result = MessageBox.Show(
                $"A new version is available: {latestTag}\nYou have {currentVersion}.\n\nDownload and restart?",
                "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes) return;

            var assets = release.Value.GetProperty("assets");
            string? downloadUrl = null;
            foreach (var asset in assets.EnumerateArray())
            {
                if (asset.GetProperty("name").GetString()?.EndsWith(".exe") == true)
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }

            if (downloadUrl is null) return;

            var currentExe = Environment.ProcessPath!;
            var tempExe = currentExe + ".new";
            var oldExe = currentExe + ".old";

            using var http = new System.Net.Http.HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("TRF.NativeClient/1.0");
            var bytes = await http.GetByteArrayAsync(downloadUrl);
            await File.WriteAllBytesAsync(tempExe, bytes);

            if (File.Exists(oldExe)) File.Delete(oldExe);
            File.Move(currentExe, oldExe);
            File.Move(tempExe, currentExe);

            System.Diagnostics.Process.Start(currentExe);
            _ = Task.Run(async () => { await Task.Delay(3000); try { File.Delete(oldExe); } catch { } });
            Application.Current.Shutdown();
        }
        catch
        {
        }
    }

    private void RefreshNav()
    {
        NavPanel.Children.Clear();
        var visibleTabs = TabPermissions.VisibleTabs(_session).Where(t => t != "Exit").ToList();

        if (_session?.Authenticated == true)
        {
            var roles = _session.Roles.Count > 0
                ? string.Join(", ", _session.Roles)
                : (_session.IsAdmin ? "admin" : "authenticated");
            StatusText.Text = roles;
        }
        else
        {
            StatusText.Text = "Not logged in";
        }

        foreach (var tab in visibleTabs)
        {
            var btn = new Button
            {
                Content = TabIcon(tab) + "  " + tab,
                Style = FindResource(tab == _currentTab ? "NavButtonActive" : "NavButton") as Style,
                Tag = tab
            };
            btn.Click += async (_, _) => await NavigateToAsync(tab);
            NavPanel.Children.Add(btn);
        }
    }

    private static string TabIcon(string tab) => tab switch
    {
        "Discord Auth" => "🔑",
        "Dashboard" => "⊞",
        "Configuration" => "⚙",
        "Message Creator" => "✉",
        "Analytics" => "📊",
        "Account" => "👤",
        "Automation" => "⚡",
        "Nation" => "🌐",
        "Alliance" => "🤝",
        "Bot Panel" => "🤖",
        "Endpoint Coverage" => "📡",
        _ => "•"
    };

    private async Task NavigateToAsync(string tab)
    {
        _currentTab = tab;
        RefreshNav();
        ContentPanel.Children.Clear();
        AddTitle(tab);

        switch (tab)
        {
            case "Discord Auth": await RenderDiscordAuthAsync(); break;
            case "Dashboard": await RenderDashboardAsync(); break;
            case "Configuration": await RenderConfigAsync(); break;
            case "Analytics": await RenderAnalyticsAsync(); break;
            case "Account": await RenderAccountAsync(); break;
            case "Automation": await RenderAutomationAsync(); break;
            case "Nation": await RenderNationsAsync(); break;
            case "Alliance": await RenderAlliancesAsync(); break;
            case "Bot Panel": await RenderBotPanelAsync(); break;
            case "Message Creator": RenderMessageCreator(); break;
            case "Endpoint Coverage": RenderEndpointCoverage(); break;
        }
    }

    private async Task RenderDiscordAuthAsync()
    {
        var card = Card();
        if (_session?.Authenticated == true)
        {
            AddLabelValue(card, "Status", "✅ Authenticated");
            AddLabelValue(card, "Admin", _session.IsAdmin ? "Yes" : "No");
            AddLabelValue(card, "Roles", _session.Roles.Count > 0 ? string.Join(", ", _session.Roles) : "(none)");
        }
        else
        {
            AddLabelValue(card, "Status", "❌ Not logged in");
        }

        var loginBtn = ActionButton("Login with Discord");
        loginBtn.Margin = new Thickness(0, 12, 0, 0);
        loginBtn.Click += async (_, _) =>
        {
            loginBtn.IsEnabled = false;
            loginBtn.Content = "Opening browser...";
            var authUrl = _client.GetDiscordAuthUrl(OAuthCallbackListener.CallbackUrl);
            var cookie = await OAuthCallbackListener.WaitForCookieAsync(authUrl);
            if (cookie is not null)
            {
                _client.Dispose();
                _client = new Bar3ApiClient(_serverUrl, Environment.GetEnvironmentVariable("BAR3_API_KEY"), cookie);
                _session = await _client.GetDiscordSessionAsync();
                RefreshNav();
                await NavigateToAsync("Discord Auth");
            }
            else
            {
                loginBtn.Content = "Login with Discord";
                loginBtn.IsEnabled = true;
            }
        };
        card.Children.Add(loginBtn);
    }

    private async Task RenderDashboardAsync()
    {
        var appData = await _client.GetAppDataAsync();
        var card = Card();
        if (appData is null) { AddError(card, "Unable to load dashboard."); return; }
        AddLabelValue(card, "Application On", appData.ApplicationOn ? "✅ Yes" : "❌ No");
        AddLabelValue(card, "Is Setup", appData.IsSetup ? "Yes" : "No");
        AddLabelValue(card, "Server Version", appData.ServerVersion);
        AddLabelValue(card, "API Usage", $"{appData.ApiDetails.Used} / {appData.ApiDetails.Max}");
        AddLabelValue(card, "Messages Sent", appData.SentMessages.Count.ToString());
    }

    private async Task RenderConfigAsync()
    {
        var config = await _client.GetConfigAsync();
        var card = Card();
        if (config is null) { AddError(card, "Unable to load configuration."); return; }
        AddLabelValue(card, "Message Subject", config.MessageSubject ?? "(not set)");
        AddLabelValue(card, "Analytics Enabled", config.AnalyticsEnabled ? "Yes" : "No");
    }

    private async Task RenderAnalyticsAsync()
    {
        var campaigns = await _client.GetAnalyticsCampaignsAsync();
        var card = Card();
        if (campaigns is null) { AddError(card, "Unable to load analytics."); return; }
        if (campaigns.Count == 0) AddInfo(card, "No analytics campaigns found.");
        else foreach (var c in campaigns) AddLabelValue(card, "Campaign", c.Name);
    }

    private async Task RenderAccountAsync()
    {
        var account = await _client.GetAccountAsync();
        var card = Card();
        if (account is null) { AddError(card, "Unable to load account."); return; }
        AddInfo(card, account.Value.ToString());
    }

    private async Task RenderAutomationAsync()
    {
        var state = await _client.GetAutomationStateAsync();
        var card = Card();
        if (state is null) { AddError(card, "Unable to load automation state."); return; }
        var enabled = state.Value.TryGetProperty("enabled", out var el) && el.GetBoolean();
        AddLabelValue(card, "Status", enabled ? "✅ Enabled" : "❌ Disabled");
        var toggleBtn = ActionButton(enabled ? "Disable Automation" : "Enable Automation");
        toggleBtn.Margin = new Thickness(0, 12, 0, 0);
        toggleBtn.Click += async (_, _) =>
        {
            toggleBtn.IsEnabled = false;
            var ok = await _client.SetAutomationStateAsync(!enabled);
            if (ok) await NavigateToAsync("Automation");
            else toggleBtn.IsEnabled = true;
        };
        card.Children.Add(toggleBtn);
    }

    private async Task RenderNationsAsync()
    {
        var nations = await _client.GetNationsAsync();
        var card = Card();
        if (nations is null) { AddError(card, "Unable to load nations."); return; }
        foreach (var n in nations.Take(20))
            AddLabelValue(card, $"#{n.NationId} {n.NationName}", $"{n.Leader} · {n.Alliance} · Cities: {n.Cities} · Score: {n.Score:0.##}");
    }

    private async Task RenderAlliancesAsync()
    {
        var alliances = await _client.GetAlliancesAsync();
        var card = Card();
        if (alliances is null) { AddError(card, "Unable to load alliances."); return; }
        foreach (var a in alliances.Take(20))
            AddLabelValue(card, $"#{a.AllianceId} {a.Name}", $"Members: {a.Members} · Score: {a.Score:0.##}");
    }

    private async Task RenderBotPanelAsync()
    {
        var serversCard = Card();
        AddSubTitle(serversCard, "Discord Servers");
        var servers = await _client.GetBotServersAsync();
        if (servers is null) AddError(serversCard, "Unable to load bot servers.");
        else if (servers.Count == 0) AddInfo(serversCard, "Bot is not in any servers.");
        else foreach (var s in servers) AddLabelValue(serversCard, s.Name, $"ID: {s.Id} · Members: {s.MemberCount:N0}");

        var cmdsCard = Card();
        AddSubTitle(cmdsCard, "Most Used Commands");
        var cmds = await _client.GetBotCommandUsageAsync();
        if (cmds is null) AddError(cmdsCard, "Unable to load command usage.");
        else if (cmds.Count == 0) AddInfo(cmdsCard, "No command data yet.");
        else foreach (var c in cmds) AddLabelValue(cmdsCard, $"/{c.Name}", $"{c.UsageCount:N0} uses · {c.Description}");

        var sendCard = Card();
        AddSubTitle(sendCard, "Send Bot Message");
        var msgBox = new TextBox { Margin = new Thickness(0, 0, 0, 8), ToolTip = "Message (max 2000 chars)" };
        var sendBtn = ActionButton("Send");
        sendBtn.Click += async (_, _) =>
        {
            var msg = msgBox.Text.Trim();
            if (string.IsNullOrEmpty(msg) || msg.Length > 2000) return;
            sendBtn.IsEnabled = false;
            var ok = await _client.SendBotMessageAsync(msg);
            MessageBox.Show(ok ? "Message sent!" : "Failed to send message.", "Bot");
            sendBtn.IsEnabled = true;
        };
        sendCard.Children.Add(msgBox);
        sendCard.Children.Add(sendBtn);
    }

    private void RenderMessageCreator()
    {
        var card = Card();
        AddSubTitle(card, "Send Message");

        var nationIdBox = new TextBox { Margin = new Thickness(0, 0, 0, 8), ToolTip = "Nation ID" };
        var nationNameBox = new TextBox { Margin = new Thickness(0, 0, 0, 8), ToolTip = "Nation Name" };
        var leaderBox = new TextBox { Margin = new Thickness(0, 0, 0, 8), ToolTip = "Leader Name" };
        var sendBtn = ActionButton("Send Message");
        sendBtn.Click += async (_, _) =>
        {
            if (!int.TryParse(nationIdBox.Text, out var nid)) return;
            sendBtn.IsEnabled = false;
            var ok = await _client.SendMessageAsync(nid, nationNameBox.Text, leaderBox.Text);
            MessageBox.Show(ok ? "Message sent!" : "Send failed.", "Message Creator");
            sendBtn.IsEnabled = true;
        };

        card.Children.Add(nationIdBox);
        card.Children.Add(nationNameBox);
        card.Children.Add(leaderBox);
        card.Children.Add(sendBtn);
    }

    private void RenderEndpointCoverage()
    {
        var card = Card();
        var endpoints = new[]
        {
            "GET  /api/appData", "GET  /api/config", "POST /api/setConfig",
            "POST /api/sendMessage", "POST /api/setApplicationState",
            "GET  /analytics/campaigns", "POST /analytics/campaigns",
            "GET  /api/nations | /nation", "GET  /api/alliances | /alliance",
            "GET  /auth/session", "GET  /account",
            "POST /api/bot/config", "GET  /api/bot/servers",
            "GET  /api/bot/commands/usage", "POST /api/bot/send",
            "POST /api/v2/auth/login", "GET  /api/v2/automation/state",
            "POST /api/v2/automation/state", "POST /api/v2/templates",
            "GET  /api/v2/analytics/me",
            "POST /api/v2/automation/send-active-unallied",
            "POST /api/v2/automation/send-active-unallied-discord",
            "POST /api/v2/automation/send-by-nation-ids",
            "GET  GitHub releases (check-for-updates)"
        };
        foreach (var ep in endpoints) AddInfo(card, ep);
    }

    private void AddTitle(string text)
    {
        ContentPanel.Children.Add(new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 238)),
            FontFamily = new FontFamily("Segoe UI Semibold"),
            FontSize = 20,
            Margin = new Thickness(0, 0, 0, 20)
        });
    }

    private static void AddSubTitle(StackPanel card, string text)
    {
        card.Children.Add(new TextBlock { Text = text, Foreground = new SolidColorBrush(Color.FromRgb(167, 139, 250)), FontFamily = new FontFamily("Segoe UI Semibold"), FontSize = 14, Margin = new Thickness(0, 0, 0, 10) });
    }
    private static void AddLabelValue(StackPanel card, string label, string value)
    {
        card.Children.Add(new TextBlock { Text = label, Foreground = new SolidColorBrush(Color.FromRgb(136, 136, 170)), FontFamily = new FontFamily("Segoe UI"), FontSize = 11, Margin = new Thickness(0, 0, 0, 2) });
        card.Children.Add(new TextBlock { Text = value, Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 238)), FontFamily = new FontFamily("Segoe UI"), FontSize = 13, Margin = new Thickness(0, 0, 0, 10), TextWrapping = TextWrapping.Wrap });
    }
    private static void AddError(StackPanel card, string msg) => card.Children.Add(new TextBlock { Text = "⚠ " + msg, Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113)), FontFamily = new FontFamily("Segoe UI"), FontSize = 13 });
    private static void AddInfo(StackPanel card, string msg) => card.Children.Add(new TextBlock { Text = msg, Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 200)), FontFamily = new FontFamily("Segoe UI"), FontSize = 13, Margin = new Thickness(0, 0, 0, 4), TextWrapping = TextWrapping.Wrap });

    private StackPanel Card()
    {
        var panel = new StackPanel();
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(22, 22, 31)),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 0, 12),
            Child = panel
        };
        ContentPanel.Children.Add(border);
        return panel;
    }

    private Button ActionButton(string text)
    {
        var style = FindResource("ActionButton") as Style;
        return new Button { Content = text, Style = style, Cursor = System.Windows.Input.Cursors.Hand };
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
