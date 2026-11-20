using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Queries;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

/// <summary>UC-09 Search, Filter and Sort Borrowings (BR-17, BR-19).</summary>
public partial class BorrowingSearchViewModel : ViewModelBase
{
    private readonly BorrowingQueryService _queryService;

    public ObservableCollection<BorrowingRowItem> Results { get; } = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    // 0 = All, 1 = Active, 2 = Returned (same order as BorrowingStatusFilter)
    [ObservableProperty]
    private int statusIndex;

    // 0 = Checkout date, 1 = Expected return date
    [ObservableProperty]
    private int sortFieldIndex;

    // 0 = Descending, 1 = Ascending
    [ObservableProperty]
    private int sortDirectionIndex;

    [ObservableProperty]
    private bool showEmptyState;

    [ObservableProperty]
    private string resultSummary = string.Empty;

    public BorrowingSearchViewModel(BorrowingQueryService queryService)
    {
        _queryService = queryService;
    }

    /// <summary>Runs the search. Named LoadAsync so the shared Retry button works too.</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var criteria = new BorrowingSearchCriteria
            {
                SearchText = SearchText,
                Status = StatusIndex switch
                {
                    1 => BorrowingStatusFilter.Active,
                    2 => BorrowingStatusFilter.Returned,
                    _ => BorrowingStatusFilter.All
                },
                SortBy = SortFieldIndex == 1 ? BorrowingSortField.ExpectedReturnDate : BorrowingSortField.CheckoutDate,
                Descending = SortDirectionIndex != 1
            };

            var records = await _queryService.SearchAsync(criteria);

            Results.Clear();
            foreach (var record in records)
            {
                Results.Add(BorrowingRowItem.From(record));
            }

            ResultSummary = Results.Count == 1 ? "1 matching borrowing" : $"{Results.Count} matching borrowings";
        }
        catch (Exception ex)
        {
            Results.Clear();
            ResultSummary = string.Empty;
            ErrorMessage = DescribeException(ex);
        }
        finally
        {
            IsBusy = false;
            // No matches is an explicit empty state, not an error (BR-17, BR-19).
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
