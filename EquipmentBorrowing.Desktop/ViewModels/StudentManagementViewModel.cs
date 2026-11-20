using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using System.Collections.ObjectModel;

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

    [ObservableProperty]
    private bool showEmptyState;

    // Selected-student history (UC-07) has its own loading / error / empty state (BR-19).
    [ObservableProperty]
    private bool isHistoryBusy;

    [ObservableProperty]
    private string? historyError;

    [ObservableProperty]
    private bool showHistoryEmpty;

    public StudentManagementViewModel(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        StudentManagementService studentService)
    {
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _studentService = studentService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var list = await _studentRepository.GetAllAsync();
            Students.Clear();
            foreach (var student in list)
                Students.Add(student);

            PrepareNewStudentForm();
        }
        catch (Exception ex)
        {
            Students.Clear();
            ErrorMessage = DescribeException(ex);
        }
        finally
        {
            IsBusy = false;
            ShowEmptyState = !HasError && Students.Count == 0;
        }
    }

    partial void OnSelectedStudentChanged(Student? value)
    {
        if (value is null)
        {
            PrepareNewStudentForm();
            SelectedStudentHistory.Clear();
            HistoryError = null;
            ShowHistoryEmpty = false;
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
        IsHistoryBusy = true;
        HistoryError = null;
        ShowHistoryEmpty = false;

        try
        {
            var history = await _studentService.GetStudentHistoryAsync(studentId);
            var equipmentMap = (await _equipmentRepository.GetAllAsync())
                .ToDictionary(e => e.Id, e => $"{e.Identifier} - {e.Name}");

            // The user may have clicked another student while this was loading: ignore stale results.
            if (SelectedStudent?.Id != studentId)
                return;

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

            ShowHistoryEmpty = SelectedStudentHistory.Count == 0;
        }
        catch (Exception ex)
        {
            SelectedStudentHistory.Clear();
            HistoryError = DescribeException(ex);
        }
        finally
        {
            IsHistoryBusy = false;
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
        try
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
                // UC-13 / UC-14: edit details and change eligibility.
                // Repeating the current values gives feedback instead of silently "saving" (BR-04).
                if (IsUnchanged(SelectedStudent))
                {
                    StatusMessage = "No changes to save: the details and eligibility are already set to these values.";
                    return;
                }

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
            StatusMessage = HasError
                ? $"Student '{savedNumber}' was saved, but the list could not be refreshed."
                : $"Student '{savedNumber}' saved successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = DescribeException(ex);
        }
    }

    private bool IsUnchanged(Student current) =>
        Student.NormalizeNumber(StudentNumberInput) == current.StudentNumber &&
        NameInput.Trim() == current.Name &&
        (string.IsNullOrWhiteSpace(ContactInfoInput) ? null : ContactInfoInput.Trim()) == current.ContactInfo &&
        IsAllowedToBorrowInput == current.IsAllowedToBorrow;
}
