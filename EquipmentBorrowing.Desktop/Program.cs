using System;
using Avalonia;

namespace EquipmentBorrowing.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things may break.
    [STAThread]
    public static void Main(string[] args)
    {
        // FIX: Enables legacy timestamp behavior so PostgreSQL handles local DateTimes cleanly
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        // A WinExe has no console, so crashes were invisible. Record them in the error log.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            ErrorLog.Write(e.ExceptionObject as Exception
                           ?? new InvalidOperationException(e.ExceptionObject?.ToString()));

        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            ErrorLog.Write(e.Exception);
            e.SetObserved();
        };

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
