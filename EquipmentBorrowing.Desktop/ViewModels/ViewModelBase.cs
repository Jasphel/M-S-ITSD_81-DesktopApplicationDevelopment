using CommunityToolkit.Mvvm.ComponentModel;

namespace EquipmentBorrowing.Desktop.ViewModels;

/// <summary>
/// Shared loading / error state so every screen can show "Loading…", a
/// understandable error with a Retry button, or an explicit empty state
/// instead of crashing (BR-18, BR-19).
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Logs the exception and returns text that is safe to show: it never contains
    /// connection strings or credentials (BR-18).
    /// </summary>
    protected static string DescribeException(Exception exception)
    {
        ErrorLog.Write(exception);

        return IsConnectionProblem(exception)
            ? "Cannot reach or use the database. Check that PostgreSQL is running, that the connection " +
              "settings are correct (see README) and that the migrations were applied, then press Retry."
            : "Something went wrong and the action was not completed. Details were saved to the error log.";
    }

    private static bool IsConnectionProblem(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is System.Data.Common.DbException ||
                current is System.Net.Sockets.SocketException ||
                current is System.IO.IOException ||
                current is TimeoutException)
            {
                return true;
            }
        }

        return false;
    }
}
