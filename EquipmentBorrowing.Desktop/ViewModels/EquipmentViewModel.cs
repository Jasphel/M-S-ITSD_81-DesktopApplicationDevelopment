using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Results;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentViewModel : ViewModelBase
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly BorrowEquipmentService _borrowEquipmentService;

    public ObservableCollection<Equipment> EquipmentList { get; } = new();
    public ObservableCollection<Student> Students { get; } = new();

    [ObservableProperty]
    private Equipment? selectedEquipment;

    [ObservableProperty]
    private Student? selectedStudent;

    [ObservableProperty]
    private DateTimeOffset? expectedReturnDate = DateTimeOffset.Now.AddDays(7);

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool showEmptyState;

    public EquipmentViewModel(
        IEquipmentRepository equipmentRepository,
        IStudentRepository studentRepository,
        BorrowEquipmentService borrowEquipmentService)
    {
        _equipmentRepository = equipmentRepository;
        _studentRepository = studentRepository;
        _borrowEquipmentService = borrowEquipmentService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var keepStudentId = SelectedStudent?.Id;

            var equipment = await _equipmentRepository.GetAllAsync();
            var students = await _studentRepository.GetAllAsync();

            EquipmentList.Clear();
            foreach (var item in equipment)
            {
                EquipmentList.Add(item);
            }

            Students.Clear();
            foreach (var student in students)
            {
                Students.Add(student);
            }

            SelectedStudent = Students.FirstOrDefault(s => s.Id == keepStudentId);
        }
        catch (Exception ex)
        {
            // Distinguish "connection failed" from "no equipment" (A01): clear the list and show the error.
            EquipmentList.Clear();
            Students.Clear();
            ErrorMessage = DescribeException(ex);
        }
        finally
        {
            IsBusy = false;
            ShowEmptyState = !HasError && EquipmentList.Count == 0;
        }
    }

    [RelayCommand]
    private async Task BorrowAsync()
    {
        // --- Presentation validation: this ViewModel's own job ---
        // Nothing here decides whether the *borrow* is allowed — only
        // whether the form was filled in well enough to even ask.
        if (SelectedStudent is null)
        {
            StatusMessage = "Please select a student.";
            return;
        }

        if (SelectedEquipment is null)
        {
            StatusMessage = "Please select a piece of equipment.";
            return;
        }

        if (ExpectedReturnDate is null)
        {
            StatusMessage = "Please choose an expected return date.";
            return;
        }

        var student = SelectedStudent;
        var item = SelectedEquipment;
        var returnDate = ExpectedReturnDate.Value.DateTime;

        try
        {
            // --- Business validation: entirely delegated (the database generates the id) ---
            var result = await _borrowEquipmentService.BorrowEquipmentAsync(
                borrowingId: 0,
                studentId: student.Id,
                equipmentId: item.Id,
                expectedReturnDate: returnDate);

            StatusMessage = result.Succeeded
                ? $"Borrowed \"{item.Name}\" for {student.Name}."
                : DescribeFailure(result.FailureReason);

            // Always refresh: availability changed, or the rejection was caused by stale data (BR-15).
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = DescribeException(ex);
        }
    }

    private static string DescribeFailure(BorrowFailureReason reason) => reason switch
    {
        BorrowFailureReason.StudentNotFound => "That student could not be found.",
        BorrowFailureReason.StudentNotAllowedToBorrow => "This student is not permitted to borrow equipment.",
        BorrowFailureReason.BorrowingLimitReached => "This student has reached the maximum of 3 active borrowings.",
        BorrowFailureReason.EquipmentNotFound => "That equipment could not be found.",
        BorrowFailureReason.EquipmentUnavailable => "This equipment is currently unavailable.",
        BorrowFailureReason.InvalidReturnDate => "The expected return date must be later than today.",
        BorrowFailureReason.Conflict => "The data changed while saving (the item may have just been borrowed). Nothing was saved and the list was refreshed.",
        BorrowFailureReason.PersistenceFailed => "The borrowing could not be saved. Nothing was changed.",
        BorrowFailureReason.OutcomeUnknown => "The connection was lost while saving, so the result is unknown. Do not retry yet: refresh and check Active Borrowings first.",
        _ => "The borrow request could not be completed."
    };
}
