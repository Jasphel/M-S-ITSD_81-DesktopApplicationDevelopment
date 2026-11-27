using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Desktop.Api;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

/// <summary>
/// UC-01 catalog, UC-02 "available only" filter, UC-03 single borrow and UC-06 multi-item checkout over HTTP.
/// </summary>
public partial class EquipmentViewModel : ViewModelBase
{
    private readonly BorrowingApiClient _api;

    public ObservableCollection<SelectableEquipment> EquipmentList { get; } = new();
    public ObservableCollection<Student> Students { get; } = new();

    [ObservableProperty]
    private Student? selectedStudent;

    [ObservableProperty]
    private DateTimeOffset? expectedReturnDate = DateTimeOffset.Now.AddDays(7);

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool showEmptyState;

    [ObservableProperty]
    private string emptyMessage = "No equipment has been registered yet.";

    [ObservableProperty]
    private string selectionSummary = "(none selected)";

    [ObservableProperty]
    private bool showOnlyAvailable;

    public EquipmentViewModel(BorrowingApiClient api)
    {
        _api = api;
    }

    partial void OnShowOnlyAvailableChanged(bool value)
    {
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var checkedIds = EquipmentList.Where(r => r.IsChecked).Select(r => r.Id).ToHashSet();
            var keepStudentId = SelectedStudent?.Id;

            var equipment = await _api.GetEquipmentAsync(ShowOnlyAvailable ? true : null);
            var students = await _api.GetStudentsAsync();

            foreach (var existing in EquipmentList)
            {
                existing.PropertyChanged -= OnRowPropertyChanged;
            }

            EquipmentList.Clear();
            foreach (var dto in equipment)
            {
                _ = int.TryParse(dto.Id, out var equipId);
                var domainItem = new Equipment(equipId, dto.EquipmentCode, dto.Name, dto.Description, dto.IsAvailable);
                var row = new SelectableEquipment(domainItem) { IsChecked = checkedIds.Contains(domainItem.Id) };
                row.PropertyChanged += OnRowPropertyChanged;
                EquipmentList.Add(row);
            }

            Students.Clear();
            foreach (var s in students)
            {
                _ = int.TryParse(s.Id, out var studentId);
                Students.Add(new Student(studentId, s.StudentNumber, s.FullName, s.Contact, s.IsEligible));
            }

            SelectedStudent = Students.FirstOrDefault(s => s.Id == keepStudentId);
            EmptyMessage = ShowOnlyAvailable
                ? "No equipment is available right now."
                : "No equipment has been registered yet.";
        }
        catch (Exception ex)
        {
            EquipmentList.Clear();
            Students.Clear();
            ErrorMessage = ex is ApiClientException ? ex.Message : DescribeException(ex);
        }
        finally
        {
            IsBusy = false;
            ShowEmptyState = !HasError && EquipmentList.Count == 0;
            UpdateSelectionSummary();
        }
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableEquipment.IsChecked))
        {
            UpdateSelectionSummary();
        }
    }

    private void UpdateSelectionSummary()
    {
        var names = EquipmentList.Where(r => r.IsChecked).Select(r => r.Name).ToList();

        SelectionSummary = names.Count == 0
            ? "(none selected)"
            : $"{names.Count} selected: {string.Join(", ", names)}";
    }

    [RelayCommand]
    private async Task BorrowAsync()
    {
        if (SelectedStudent is null)
        {
            StatusMessage = "Please select a student.";
            return;
        }

        var ticked = EquipmentList.Where(r => r.IsChecked).ToList();
        if (ticked.Count == 0)
        {
            StatusMessage = "Tick at least one piece of equipment.";
            return;
        }

        if (ExpectedReturnDate is null)
        {
            StatusMessage = "Please choose an expected return date.";
            return;
        }

        var studentIdStr = SelectedStudent.Id.ToString(CultureInfo.InvariantCulture);
        var studentName = SelectedStudent.Name;
        var dueDateStr = ExpectedReturnDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        try
        {
            if (ticked.Count == 1)
            {
                var equipIdStr = ticked[0].Id.ToString(CultureInfo.InvariantCulture);
                await _api.BorrowSingleAsync(studentIdStr, equipIdStr, dueDateStr);
                StatusMessage = $"Borrowed \"{ticked[0].Name}\" for {studentName}.";
            }
            else
            {
                var equipIds = ticked.Select(r => r.Id.ToString(CultureInfo.InvariantCulture)).ToArray();
                var created = await _api.CheckoutMultipleAsync(studentIdStr, equipIds, dueDateStr);
                StatusMessage = $"Checked out {created.Count} items for {studentName}: {string.Join(", ", ticked.Select(r => r.Name))}.";
            }

            foreach (var row in ticked)
            {
                row.IsChecked = false;
            }

            await LoadAsync();
        }
        catch (ApiClientException ex)
        {
            var scope = ticked.Count > 1 ? " Nothing was checked out." : string.Empty;
            StatusMessage = ex.IsUnknownOutcome
                ? "The connection was lost while saving, so the result is unknown. Do not retry yet: refresh and check Active Borrowings first."
                : ex.Message + scope;

            if (!ex.IsUnreachable && !ex.IsUnknownOutcome)
            {
                await LoadAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = DescribeException(ex);
        }
    }
}