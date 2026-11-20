using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Results;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

/// <summary>
/// UC-01 catalog, UC-02 "available only" filter, UC-03 single borrow and UC-06 multi-item checkout.
/// </summary>
public partial class EquipmentViewModel : ViewModelBase
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly BorrowEquipmentService _borrowEquipmentService;
    private readonly CheckoutMultipleEquipmentService _checkoutService;

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

    // UC-02: when ticked, only currently available items are listed.
    [ObservableProperty]
    private bool showOnlyAvailable;

    public EquipmentViewModel(
        IEquipmentRepository equipmentRepository,
        IStudentRepository studentRepository,
        BorrowEquipmentService borrowEquipmentService,
        CheckoutMultipleEquipmentService checkoutService)
    {
        _equipmentRepository = equipmentRepository;
        _studentRepository = studentRepository;
        _borrowEquipmentService = borrowEquipmentService;
        _checkoutService = checkoutService;
    }

    partial void OnShowOnlyAvailableChanged(bool value)
    {
        // Re-read the current state from PostgreSQL, then filter (UC-02 step 2-3).
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

            var equipment = await _equipmentRepository.GetAllAsync();
            var students = await _studentRepository.GetAllAsync();

            EquipmentList.Clear();
            foreach (var item in equipment)
            {
                if (ShowOnlyAvailable && !item.IsAvailable)
                {
                    continue;
                }

                var row = new SelectableEquipment(item) { IsChecked = checkedIds.Contains(item.Id) };
                row.PropertyChanged += OnRowPropertyChanged;
                EquipmentList.Add(row);
            }

            Students.Clear();
            foreach (var student in students)
            {
                Students.Add(student);
            }

            SelectedStudent = Students.FirstOrDefault(s => s.Id == keepStudentId);
            EmptyMessage = ShowOnlyAvailable
                ? "No equipment is available right now."
                : "No equipment has been registered yet.";
        }
        catch (Exception ex)
        {
            // "Connection failed" must look different from "no equipment" (A01, A02).
            EquipmentList.Clear();
            Students.Clear();
            ErrorMessage = DescribeException(ex);
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
        // --- Presentation validation: this ViewModel's own job ---
        // Nothing here decides whether the borrow is allowed, only
        // whether the form was filled in well enough to even ask.
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

        var student = SelectedStudent;
        var returnDate = ExpectedReturnDate.Value.DateTime;

        try
        {
            // --- Business validation: entirely delegated ---
            BorrowResult result;
            if (ticked.Count == 1)
            {
                // UC-03: the database generates the borrowing id.
                result = await _borrowEquipmentService.BorrowEquipmentAsync(
                    borrowingId: 0,
                    studentId: student.Id,
                    equipmentId: ticked[0].Id,
                    expectedReturnDate: returnDate);
            }
            else
            {
                // UC-06: all or nothing.
                result = await _checkoutService.CheckOutAsync(
                    student.Id,
                    ticked.Select(r => r.Id).ToList(),
                    returnDate);
            }

            if (result.Succeeded)
            {
                foreach (var row in ticked)
                {
                    row.IsChecked = false;
                }

                StatusMessage = ticked.Count == 1
                    ? $"Borrowed \"{ticked[0].Name}\" for {student.Name}."
                    : $"Checked out {ticked.Count} items for {student.Name}: {string.Join(", ", ticked.Select(r => r.Name))}.";
            }
            else
            {
                StatusMessage = DescribeFailure(result, ticked);
            }

            // Always refresh: availability changed, or the rejection was caused by stale data (BR-15).
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = DescribeException(ex);
        }
    }

    private static string DescribeFailure(BorrowResult result, IReadOnlyList<SelectableEquipment> ticked)
    {
        var offending = ticked.FirstOrDefault(r => r.Id == result.OffendingEquipmentId);
        var itemText = offending is null ? "An item" : $"\"{offending.Name}\" ({offending.Identifier})";
        var scope = ticked.Count > 1 ? " Nothing was checked out." : string.Empty;

        return result.FailureReason switch
        {
            BorrowFailureReason.StudentNotFound => "That student could not be found.",
            BorrowFailureReason.StudentNotAllowedToBorrow => "This student is not permitted to borrow equipment." + scope,
            BorrowFailureReason.BorrowingLimitReached => "This request would exceed the limit of 3 active borrowed items per student." + scope,
            BorrowFailureReason.EquipmentNotFound => itemText + " could not be found." + scope,
            BorrowFailureReason.EquipmentUnavailable => itemText + " is currently unavailable." + scope,
            BorrowFailureReason.InvalidReturnDate => "The expected return date must be later than today." + scope,
            BorrowFailureReason.NotEnoughItems => "A multi-item checkout needs at least two items.",
            BorrowFailureReason.DuplicateItems => "The same item was selected more than once." + scope,
            BorrowFailureReason.Conflict => "The data changed while saving (an item may have just been borrowed). Nothing was saved and the list was refreshed.",
            BorrowFailureReason.PersistenceFailed => "The borrowing could not be saved. Nothing was changed.",
            BorrowFailureReason.OutcomeUnknown => "The connection was lost while saving, so the result is unknown. Do not retry yet: refresh and check Active Borrowings first.",
            _ => "The borrow request could not be completed."
        };
    }
}
