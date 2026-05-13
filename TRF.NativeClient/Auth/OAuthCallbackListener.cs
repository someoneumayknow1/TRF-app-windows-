using System.Net;
using System.Net.Sockets;

namespace TRF.NativeClient.Auth;

public static class OAuthCallbackListener
{
    private const string CallbackPath = "/callback";

    public static string CreateCallbackUrl()
    {
        using var tcp = new TcpListener(IPAddress.Loopback, 0);
        tcp.Start();
        var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
        return $"http://localhost:{port}{CallbackPath}";
    }

    public static async Task<string?> WaitForCookieAsync(
        string discordAuthUrl,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromMinutes(3);
        var callbackUrl = CreateCallbackUrl();
        var callbackUri = new Uri(callbackUrl);
        var port = callbackUri.Port;

        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"Unable to start OAuth callback listener on http://localhost:{port}/ ({ex.Message})");
            return null;
        }

        var authUrl = EnsureReturnTo(discordAuthUrl, callbackUrl);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(authUrl)
        {
            UseShellExecute = true
        });

        Console.WriteLine($"Browser opened. Waiting for Discord login (timeout: {timeout.Value.TotalMinutes:0} min)...");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout.Value);

        try
        {
            var contextTask = listener.GetContextAsync();
            await Task.WhenAny(contextTask, Task.Delay(Timeout.Infinite, cts.Token));

            if (!contextTask.IsCompletedSuccessfully)
            {
                listener.Stop();
                Console.WriteLine("Login timed out.");
                return null;
            }

            var context = contextTask.Result;

            var response = context.Response;
            var html = "<html><body><h2>Logged in! You can close this tab and return to the app.</h2></body></html>"u8.ToArray();
            response.ContentType = "text/html";
            response.ContentLength64 = html.Length;
            await response.OutputStream.WriteAsync(html, cancellationToken);
            response.Close();

            var cookie = context.Request.Headers["Cookie"];
            if (!string.IsNullOrWhiteSpace(cookie))
            {
                return cookie;
            }

            var queryCookie = context.Request.QueryString["cookie"];
            return string.IsNullOrWhiteSpace(queryCookie) ? null : queryCookie;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Login cancelled.");
            return null;
        }
        finally
        {
            listener.Stop();
        }
    }

    private static string EnsureReturnTo(string authUrl, string callbackUrl)
    {
        if (!Uri.TryCreate(authUrl, UriKind.Absolute, out var authUri))
        {
            var separator = authUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            return $"{authUrl}{separator}returnTo={Uri.EscapeDataString(callbackUrl)}";
        }

        var builder = new UriBuilder(authUri);
        var queryParts = builder.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.StartsWith("returnTo=", StringComparison.OrdinalIgnoreCase))
            .ToList();
        queryParts.Add($"returnTo={Uri.EscapeDataString(callbackUrl)}");
        builder.Query = string.Join("&", queryParts);
        return builder.Uri.ToString();
    }
}
