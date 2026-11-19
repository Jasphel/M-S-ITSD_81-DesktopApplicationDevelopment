using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using System.Collections.ObjectModel;
using System.Reflection.PortableExecutable;

namespace EquipmentBorrowing.Desktop.ViewModels;

public sealed record StudentHistoryItem(
    int BorrowingId,
    string EquipmentLabel,
    string DateBorrowedText,
    string ExpectedReturnText,
    string ActualReturnText,
    string StatusText,
    bool IsReturned);

public partial class StudentManagementViewModel : ViewModelBase
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly StudentManagementService _studentService;

    public ObservableCollection<Student> Students { get; } = new();
    public ObservableCollection<StudentHistoryItem> SelectedStudentHistory { get; } = new();

    [ObservableProperty]
    private Student? selectedStudent;

    [ObservableProperty]
    private string studentNumberInput = string.Empty;

    [ObservableProperty]
    private string nameInput = string.Empty;

    [ObservableProperty]
    private string contactInfoInput = string.Empty;

    [ObservableProperty]
    private bool isAllowedToBorrowInput = true;

    [ObservableProperty]
    private string formHeader = "Register New Student";

    [ObservableProperty]
    private string saveButtonText = "Register Student";

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public StudentManagementViewModel(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        StudentManagementService studentService)
    {
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _studentService = studentService;
    }

    public async Task LoadAsync()
    {
        var list = await _studentRepository.GetAllAsync();
        Students.Clear();
        foreach (var student in list)
            Students.Add(student);

        PrepareNewStudentForm();
    }

    partial void OnSelectedStudentChanged(Student? value)
    {
        if (value is null)
        {
            PrepareNewStudentForm();
            SelectedStudentHistory.Clear();
            return;
        }

        FormHeader = $"Edit Student (#{value.Id})";
        SaveButtonText = "Save Changes";
        StudentNumberInput = value.StudentNumber;
        NameInput = value.Name;
        ContactInfoInput = value.ContactInfo ?? string.Empty;
        IsAllowedToBorrowInput = value.IsAllowedToBorrow;
        StatusMessage = string.Empty;

        _ = LoadHistoryForStudentAsync(value.Id);
    }

    private async Task LoadHistoryForStudentAsync(int studentId)
    {
        var history = await _studentService.GetStudentHistoryAsync(studentId);
        var equipmentMap = (await _equipmentRepository.GetAllAsync())
            .ToDictionary(e => e.Id, e => $"{e.Identifier} - {e.Name}");

        SelectedStudentHistory.Clear();
        foreach (var b in history)
        {
            var equipLabel = equipmentMap.TryGetValue(b.EquipmentId, out var label)
                ? label
                : $"Equipment #{b.EquipmentId}";

            SelectedStudentHistory.Add(new StudentHistoryItem(
                b.Id,
                equipLabel,
                b.DateBorrowed.ToString("yyyy-MM-dd"),
                b.ExpectedReturnDate.ToString("yyyy-MM-dd"),
                b.ActualReturnDate?.ToString("yyyy-MM-dd") ?? "—",
                b.Status.ToString(),
                b.Status == BorrowingStatus.Returned));
        }
    }

    [RelayCommand]
    private void StartNewStudent()
    {
        SelectedStudent = null;
        PrepareNewStudentForm();
    }

    private void PrepareNewStudentForm()
    {
        FormHeader = "Register New Student";
        SaveButtonText = "Register Student";
        StudentNumberInput = string.Empty;
        NameInput = string.Empty;
        ContactInfoInput = string.Empty;
        IsAllowedToBorrowInput = true;
    }

    [RelayCommand]
    private async Task SaveStudentAsync()
    {
        OperationResult result;
        if (SelectedStudent is null)
        {
            // UC-12: Register Student
            result = await _studentService.RegisterStudentAsync(
                StudentNumberInput,
                NameInput,
                ContactInfoInput,
                IsAllowedToBorrowInput);
        }
        else
        {
            // UC-13: Edit Student
            result = await _studentService.UpdateStudentAsync(
                SelectedStudent.Id,
                StudentNumberInput,
                NameInput,
                ContactInfoInput,
                IsAllowedToBorrowInput);
        }

        if (!result.IsSuccess)
        {
            StatusMessage = result.ErrorMessage ?? "Operation failed.";
            return;
        }

        var savedNumber = Student.NormalizeNumber(StudentNumberInput);
        await LoadAsync();
        StatusMessage = $"Student '{savedNumber}' saved successfully.";
    }
}