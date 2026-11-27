using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Desktop.Api;
using EquipmentBorrowing.Domain;

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
    private readonly BorrowingApiClient _api;

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

    [ObservableProperty]
    private bool isHistoryBusy;

    [ObservableProperty]
    private string? historyError;

    [ObservableProperty]
    private bool showHistoryEmpty;

    public StudentManagementViewModel(BorrowingApiClient api)
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
            var list = await _api.GetStudentsAsync();
            Students.Clear();
            foreach (var dto in list)
            {
                Students.Add(MapStudent(dto));
            }

            PrepareNewStudentForm();
        }
        catch (Exception ex)
        {
            Students.Clear();
            ErrorMessage = ex is ApiClientException ? ex.Message : DescribeException(ex);
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
            var history = await _api.GetStudentBorrowingsAsync(studentId.ToString(CultureInfo.InvariantCulture));

            if (SelectedStudent?.Id != studentId)
                return;

            SelectedStudentHistory.Clear();
            foreach (var b in history)
            {
                _ = int.TryParse(b.Id, out var borrowId);
                var equipLabel = string.IsNullOrWhiteSpace(b.EquipmentCode)
                    ? b.EquipmentName
                    : $"{b.EquipmentCode} - {b.EquipmentName}";

                SelectedStudentHistory.Add(new StudentHistoryItem(
                    borrowId,
                    equipLabel,
                    b.BorrowedDate,
                    b.ExpectedReturnDate,
                    string.IsNullOrWhiteSpace(b.ReturnedDate) ? "—" : b.ReturnedDate,
                    b.Status,
                    string.Equals(b.Status, "Returned", StringComparison.OrdinalIgnoreCase)));
            }

            ShowHistoryEmpty = SelectedStudentHistory.Count == 0;
        }
        catch (Exception ex)
        {
            SelectedStudentHistory.Clear();
            HistoryError = ex is ApiClientException ? ex.Message : DescribeException(ex);
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
            string? contact = string.IsNullOrWhiteSpace(ContactInfoInput) ? null : ContactInfoInput.Trim();

            if (SelectedStudent is null)
            {
                var created = await _api.CreateStudentAsync(
                    StudentNumberInput.Trim(),
                    NameInput.Trim(),
                    contact);

                if (IsAllowedToBorrowInput != created.IsEligible)
                {
                    await _api.SetStudentEligibilityAsync(created.Id, IsAllowedToBorrowInput);
                }

                await LoadAsync();
                StatusMessage = HasError
                    ? $"Student '{created.StudentNumber}' was saved, but the list could not be refreshed."
                    : $"Student '{created.StudentNumber}' registered successfully.";
            }
            else
            {
                if (IsUnchanged(SelectedStudent))
                {
                    StatusMessage = "No changes to save: the details and eligibility are already set to these values.";
                    return;
                }

                var idText = SelectedStudent.Id.ToString(CultureInfo.InvariantCulture);
                var detailsChanged =
                    Student.NormalizeNumber(StudentNumberInput) != SelectedStudent.StudentNumber ||
                    NameInput.Trim() != SelectedStudent.Name ||
                    contact != SelectedStudent.ContactInfo;

                if (detailsChanged)
                {
                    await _api.UpdateStudentAsync(
                        idText,
                        StudentNumberInput.Trim(),
                        NameInput.Trim(),
                        contact);
                }

                if (IsAllowedToBorrowInput != SelectedStudent.IsAllowedToBorrow)
                {
                    await _api.SetStudentEligibilityAsync(idText, IsAllowedToBorrowInput);
                }

                var savedNumber = Student.NormalizeNumber(StudentNumberInput);
                await LoadAsync();
                StatusMessage = HasError
                    ? $"Student '{savedNumber}' was updated, but the list could not be refreshed."
                    : $"Student '{savedNumber}' updated successfully.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = ex is ApiClientException ? ex.Message : DescribeException(ex);
        }
    }

    private bool IsUnchanged(Student current) =>
        Student.NormalizeNumber(StudentNumberInput) == current.StudentNumber &&
        NameInput.Trim() == current.Name &&
        (string.IsNullOrWhiteSpace(ContactInfoInput) ? null : ContactInfoInput.Trim()) == current.ContactInfo &&
        IsAllowedToBorrowInput == current.IsAllowedToBorrow;

    private static Student MapStudent(ApiStudentDto dto)
    {
        _ = int.TryParse(dto.Id, out var id);
        return new Student(id, dto.StudentNumber, dto.FullName, dto.Contact, dto.IsEligible);
    }
}