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
        tcp.Stop();
        return $"http://localhost:{port}{CallbackPath}";
    }

    public static async Task<string?> WaitForCookieAsync(
        string discordAuthUrl,
        string? callbackUrl = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromMinutes(3);
        if (!TryCreateStartedListener(callbackUrl, out var listener, out var finalCallbackUrl))
        {
            Console.WriteLine("Unable to start OAuth callback listener on localhost.");
            return null;
        }

        using (listener)
        {
            try
            {
                var authUrl = EnsureReturnTo(discordAuthUrl, finalCallbackUrl);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(authUrl)
                {
                    UseShellExecute = true
                });

                Console.WriteLine($"Browser opened. Waiting for Discord login (timeout: {timeout.Value.TotalMinutes:0} min)...");

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout.Value);

                var context = await listener.GetContextAsync().WaitAsync(cts.Token);

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
                Console.WriteLine(cancellationToken.IsCancellationRequested ? "Login cancelled." : "Login timed out.");
                return null;
            }
            finally
            {
                listener.Close();
            }
        }
    }

    private static string EnsureReturnTo(string authUrl, string callbackUrl)
    {
        if (!Uri.TryCreate(authUrl, UriKind.Absolute, out var authUri))
        {
            return authUrl;
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

    private static bool TryCreateStartedListener(string? preferredCallbackUrl, out HttpListener listener, out string callbackUrl)
    {
        if (TryStartListener(preferredCallbackUrl, out listener, out callbackUrl))
        {
            return true;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            if (TryStartListener(CreateCallbackUrl(), out listener, out callbackUrl))
            {
                return true;
            }
        }

        listener = new HttpListener();
        callbackUrl = string.Empty;
        return false;
    }

    private static bool TryStartListener(string? candidateCallbackUrl, out HttpListener listener, out string callbackUrl)
    {
        listener = new HttpListener();
        callbackUrl = string.Empty;

        if (string.IsNullOrWhiteSpace(candidateCallbackUrl) ||
            !Uri.TryCreate(candidateCallbackUrl, UriKind.Absolute, out var callbackUri) ||
            !callbackUri.IsLoopback)
        {
            return false;
        }

        listener.Prefixes.Add($"http://localhost:{callbackUri.Port}/");

        try
        {
            listener.Start();
            callbackUrl = candidateCallbackUrl;
            return true;
        }
        catch (HttpListenerException)
        {
            listener.Close();
            listener = new HttpListener();
            return false;
        }
    }
}
