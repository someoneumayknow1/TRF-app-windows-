using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace TRF.NativeClient.Auth;

public class DiscordAuthWindow : Window
{
    private readonly WebView2 _webView = new();
    private readonly TaskCompletionSource<string?> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Uri _callbackUri;
    private readonly Uri _cookieScopeUri;
    private bool _isCompleted;

    public DiscordAuthWindow(string discordAuthUrl, string callbackUrl)
    {
        _callbackUri = new Uri(callbackUrl);
        _cookieScopeUri = new Uri(discordAuthUrl);

        Title = "Discord Login";
        Width = 520;
        Height = 760;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = _webView;

        Loaded += async (_, _) =>
        {
            try
            {
                await _webView.EnsureCoreWebView2Async();
                _webView.CoreWebView2.NavigationStarting += HandleNavigationStarting;
                _webView.Source = new Uri(discordAuthUrl);
            }
            catch
            {
                Complete(null);
            }
        };

        Closed += (_, _) => Complete(null);
    }

    public Task<string?> AuthenticateAsync()
    {
        ShowDialog();
        return _completionSource.Task;
    }

    private async void HandleNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var navigationUri))
        {
            return;
        }

        if (!IsCallbackNavigation(navigationUri))
        {
            return;
        }

        e.Cancel = true;

        try
        {
            var cookieUri = _cookieScopeUri.GetLeftPart(UriPartial.Authority);
            var cookies = await _webView.CoreWebView2.CookieManager.GetCookiesAsync(cookieUri);
            var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
            Complete(string.IsNullOrWhiteSpace(cookieHeader) ? null : cookieHeader);
        }
        catch
        {
            Complete(null);
        }
    }

    private bool IsCallbackNavigation(Uri uri) =>
        string.Equals(uri.Scheme, _callbackUri.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(uri.Host, _callbackUri.Host, StringComparison.OrdinalIgnoreCase) &&
        uri.Port == _callbackUri.Port &&
        uri.AbsolutePath.StartsWith(_callbackUri.AbsolutePath, StringComparison.OrdinalIgnoreCase);

    private void Complete(string? cookie)
    {
        if (_isCompleted)
        {
            return;
        }

        _isCompleted = true;
        _completionSource.TrySetResult(cookie);

        if (IsVisible)
        {
            Close();
        }
    }
}
