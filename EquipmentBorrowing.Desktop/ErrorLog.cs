namespace EquipmentBorrowing.Desktop;

/// <summary>
/// Writes unexpected exceptions to a local log file. The app is a WinExe, so
/// "dotnet run" shows nothing in the terminal; this file is where the details go.
/// Location: %LOCALAPPDATA%\EquipmentBorrowing\error.log
/// </summary>
public static class ErrorLog
{
    public static string LogPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "EquipmentBorrowing",
        "error.log");

    public static void Write(Exception exception)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(
                LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never crash the app.
        }
    }
}
