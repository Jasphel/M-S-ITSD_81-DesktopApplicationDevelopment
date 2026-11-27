using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Desktop.Api;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingsViewModel : ViewModelBase
{
    private readonly BorrowingApiClient _api;

    public ObservableCollection<BorrowingListItem> ActiveBorrowings { get; } = new();

    [ObservableProperty]
    private BorrowingListItem? selectedBorrowing;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool showEmptyState;

    public BorrowingsViewModel(BorrowingApiClient api)
    {
        _api = api;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var active = await _api.SearchBorrowingsAsync(status: "Active");

            ActiveBorrowings.Clear();

            foreach (var b in active)
            {
                _ = int.TryParse(b.Id, out var id);
                _ = DateTime.TryParse(b.BorrowedDate, out var borrowedDt);
                _ = DateTime.TryParse(b.ExpectedReturnDate, out var expectedDt);

                ActiveBorrowings.Add(new BorrowingListItem
                {
                    Id = id,
                    StudentName = string.IsNullOrWhiteSpace(b.StudentName) ? "Unknown student" : b.StudentName,
                    EquipmentName = string.IsNullOrWhiteSpace(b.EquipmentName) ? "Unknown equipment" : b.EquipmentName,
                    DateBorrowed = borrowedDt,
                    ExpectedReturnDate = expectedDt
                });
            }
        }
        catch (Exception ex)
        {
            ActiveBorrowings.Clear();
            ErrorMessage = ex is ApiClientException ? ex.Message : DescribeException(ex);
        }
        finally
        {
            IsBusy = false;
            ShowEmptyState = !HasError && ActiveBorrowings.Count == 0;
        }
    }

    [RelayCommand]
    private async Task ReturnAsync()
    {
        if (SelectedBorrowing is null)
        {
            StatusMessage = "Please select a borrowing to return.";
            return;
        }

        var selected = SelectedBorrowing;

        try
        {
            await _api.ReturnBorrowingAsync(selected.Id.ToString(CultureInfo.InvariantCulture));
            StatusMessage = $"Returned \"{selected.EquipmentName}\".";
            SelectedBorrowing = null;
            await LoadAsync();
        }
        catch (ApiClientException ex)
        {
            StatusMessage = ex.IsUnknownOutcome
                ? "The connection was lost while saving, so the result is unknown. Refresh and check the list before trying again."
                : ex.Message;

            if (!ex.IsUnreachable && !ex.IsUnknownOutcome)
            {
                SelectedBorrowing = null;
                await LoadAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = DescribeException(ex);
        }
    }
}