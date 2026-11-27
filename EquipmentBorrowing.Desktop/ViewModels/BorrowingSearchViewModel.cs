using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Queries;
using EquipmentBorrowing.Desktop.Api;
using EquipmentBorrowing.Domain;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace EquipmentBorrowing.Desktop.ViewModels;

/// <summary>Search, Filter and Sort Borrowings over HTTP.</summary>
public partial class BorrowingSearchViewModel : ViewModelBase
{
    private readonly BorrowingApiClient _api;

    public ObservableCollection<BorrowingRowItem> Results { get; } = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int statusIndex;

    [ObservableProperty]
    private int sortFieldIndex;

    [ObservableProperty]
    private int sortDirectionIndex;

    [ObservableProperty]
    private bool showEmptyState;

    [ObservableProperty]
    private string resultSummary = string.Empty;

    public BorrowingSearchViewModel(BorrowingApiClient api)
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
            var status = StatusIndex switch
            {
                1 => "Active",
                2 => "Returned",
                _ => "All"
            };
            var sort = SortFieldIndex == 1 ? "expectedReturnDate" : "borrowedDate";
            var direction = SortDirectionIndex == 1 ? "asc" : "desc";

            var records = await _api.SearchBorrowingsAsync(
                search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                status: status,
                sort: sort,
                direction: direction);

            Results.Clear();
            foreach (var b in records)
            {
                _ = int.TryParse(b.Id, out var borrowId);
                _ = int.TryParse(b.StudentId, out var studentId);
                _ = int.TryParse(b.EquipmentId, out var eqId);
                _ = DateTime.TryParse(b.BorrowedDate, out var borrowedDt);
                _ = DateTime.TryParse(b.ExpectedReturnDate, out var expectedDt);
                DateTime? returnedDt = DateTime.TryParse(b.ReturnedDate, out var parsedRet) ? parsedRet : null;
                Enum.TryParse<BorrowingStatus>(b.Status, true, out var parsedStatus);

                Results.Add(BorrowingRowItem.From(new BorrowingRecord
                {
                    BorrowingId = borrowId,
                    StudentId = studentId,
                    StudentNumber = b.StudentNumber,
                    StudentName = b.StudentName,
                    EquipmentId = eqId,
                    EquipmentIdentifier = b.EquipmentCode,
                    EquipmentName = b.EquipmentName,
                    DateBorrowed = borrowedDt,
                    ExpectedReturnDate = expectedDt,
                    ActualReturnDate = returnedDt,
                    Status = parsedStatus
                }));
            }

            ResultSummary = Results.Count == 1 ? "1 matching borrowing" : $"{Results.Count} matching borrowings";
        }
        catch (Exception ex)
        {
            Results.Clear();
            ResultSummary = string.Empty;
            ErrorMessage = ex is ApiClientException ? ex.Message : DescribeException(ex);
        }
        finally
        {
            IsBusy = false;
            ShowEmptyState = !HasError && Results.Count == 0;
        }
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        SearchText = string.Empty;
        StatusIndex = 0;
        SortFieldIndex = 0;
        SortDirectionIndex = 0;
        await LoadAsync();
    }
}