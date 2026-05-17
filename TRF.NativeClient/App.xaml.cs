using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TRF.NativeClient;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            File.WriteAllText("D:\\stefan\\trf_crash.txt", $"{args.Exception}");
            args.Handled = true;
            Shutdown();
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            File.WriteAllText("D:\\stefan\\trf_crash.txt", args.ExceptionObject?.ToString() ?? "null");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            File.WriteAllText("D:\\stefan\\trf_crash.txt", args.Exception?.ToString() ?? "null");
            args.SetObserved();
        };
    }
}
