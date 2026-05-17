using System;
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
            MessageBox.Show(
                $"Unhandled error:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "TRF Client Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            MessageBox.Show(
                $"Fatal error:\n\n{args.ExceptionObject}",
                "TRF Client Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };
    }
}
