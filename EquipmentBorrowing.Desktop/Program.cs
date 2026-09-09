using Avalonia;

namespace EquipmentBorrowing.Desktop;

internal static class Program
{
    // This is the actual entry point for the desktop application.
    // Avalonia needs a plain, static Main — the DI composition happens
    // later, inside App.axaml.cs, once the framework has started.
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
