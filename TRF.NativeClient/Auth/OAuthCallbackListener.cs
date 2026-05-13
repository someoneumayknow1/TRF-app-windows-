using System.Net;

namespace TRF.NativeClient.Auth;

public static class OAuthCallbackListener
{
    private const int Port = 9876;
    public static readonly string CallbackUrl = $"http://localhost:{Port}/callback";

    public static async Task<string?> WaitForCookieAsync(
        string discordAuthUrl,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromMinutes(3);

        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{Port}/");
        listener.Start();

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(discordAuthUrl)
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
}
