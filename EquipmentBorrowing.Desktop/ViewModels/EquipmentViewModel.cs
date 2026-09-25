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
    private readonly IBorrowingRepository _borrowingRepository;
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

    public EquipmentViewModel(
        IEquipmentRepository equipmentRepository,
        IStudentRepository studentRepository,
        IBorrowingRepository borrowingRepository,
        BorrowEquipmentService borrowEquipmentService)
    {
        _equipmentRepository = equipmentRepository;
        _studentRepository = studentRepository;
        _borrowingRepository = borrowingRepository;
        _borrowEquipmentService = borrowEquipmentService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
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

        if (ExpectedReturnDate is null || ExpectedReturnDate.Value.Date < DateTime.Now.Date)
        {
            StatusMessage = "Please choose a valid expected return date.";
            return;
        }

        // --- Business validation: entirely delegated ---
        var nextId = await _borrowingRepository.GetNextIdAsync();

        var result = await _borrowEquipmentService.BorrowEquipmentAsync(
            borrowingId: nextId,
            studentId: SelectedStudent.Id,
            equipmentId: SelectedEquipment.Id,
            expectedReturnDate: ExpectedReturnDate.Value.DateTime);

        StatusMessage = result.Succeeded
            ? $"Borrowed \"{SelectedEquipment.Name}\" for {SelectedStudent.Name}."
            : DescribeFailure(result.FailureReason);

        if (result.Succeeded)
        {
            await LoadAsync(); // equipment availability changed
            SelectedEquipment = null;
        }
    }

    private static string DescribeFailure(BorrowFailureReason reason) => reason switch
    {
        BorrowFailureReason.StudentNotFound => "That student could not be found.",
        BorrowFailureReason.StudentNotAllowedToBorrow => "This student is not permitted to borrow equipment.",
        BorrowFailureReason.BorrowingLimitReached => "This student has reached the maximum number of active borrowings.",
        BorrowFailureReason.EquipmentNotFound => "That equipment could not be found.",
        BorrowFailureReason.EquipmentUnavailable => "This equipment is currently unavailable.",
        _ => "The borrow request could not be completed."
    };
}
