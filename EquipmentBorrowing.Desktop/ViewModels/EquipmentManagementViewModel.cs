using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Queries;
using EquipmentBorrowing.Desktop.Api;
using EquipmentBorrowing.Domain;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentManagementViewModel : ViewModelBase
{
    private readonly BorrowingApiClient _api;

    public ObservableCollection<Equipment> EquipmentList { get; } = new();
    public ObservableCollection<BorrowingRowItem> SelectedEquipmentHistory { get; } = new();

    [ObservableProperty]
    private Equipment? selectedEquipment;

    [ObservableProperty]
    private string identifierInput = string.Empty;

    [ObservableProperty]
    private string nameInput = string.Empty;

    [ObservableProperty]
    private string descriptionInput = string.Empty;

    [ObservableProperty]
    private string availabilityStatusText = "Available (new item)";

    [ObservableProperty]
    private string formHeader = "Add New Equipment";

    [ObservableProperty]
    private string saveButtonText = "Add Equipment";

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

    public EquipmentManagementViewModel(BorrowingApiClient api)
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
            var list = await _api.GetEquipmentAsync();

            EquipmentList.Clear();
            foreach (var dto in list)
            {
                _ = int.TryParse(dto.Id, out var id);
                EquipmentList.Add(new Equipment(id, dto.EquipmentCode, dto.Name, dto.Description, dto.IsAvailable));
            }

            PrepareNewEquipmentForm();
        }
        catch (Exception ex)
        {
            EquipmentList.Clear();
            ErrorMessage = ex is ApiClientException ? ex.Message : DescribeException(ex);
        }
        finally
        {
            IsBusy = false;
            ShowEmptyState = !HasError && EquipmentList.Count == 0;
        }
    }

    partial void OnSelectedEquipmentChanged(Equipment? value)
    {
        if (value is null)
        {
            PrepareNewEquipmentForm();
            SelectedEquipmentHistory.Clear();
            HistoryError = null;
            ShowHistoryEmpty = false;
            return;
        }

        FormHeader = $"Edit Equipment (#{value.Id})";
        SaveButtonText = "Save Changes";
        IdentifierInput = value.Identifier;
        NameInput = value.Name;
        DescriptionInput = value.Description ?? string.Empty;
        AvailabilityStatusText = value.IsAvailable
            ? "Available (managed via Borrow/Return)"
            : "Currently Borrowed (managed via Return)";
        StatusMessage = string.Empty;

        _ = LoadHistoryForEquipmentAsync(value.Id);
    }

    private async Task LoadHistoryForEquipmentAsync(int equipmentId)
    {
        IsHistoryBusy = true;
        HistoryError = null;
        ShowHistoryEmpty = false;

        try
        {
            var records = await _api.GetEquipmentBorrowingsAsync(equipmentId.ToString(CultureInfo.InvariantCulture));

            if (SelectedEquipment?.Id != equipmentId)
                return;

            SelectedEquipmentHistory.Clear();
            foreach (var b in records)
            {
                _ = int.TryParse(b.Id, out var borrowId);
                _ = int.TryParse(b.StudentId, out var studentId);
                _ = int.TryParse(b.EquipmentId, out var eqId);
                _ = DateTime.TryParse(b.BorrowedDate, out var borrowedDt);
                _ = DateTime.TryParse(b.ExpectedReturnDate, out var expectedDt);
                DateTime? returnedDt = DateTime.TryParse(b.ReturnedDate, out var parsedRet) ? parsedRet : null;
                Enum.TryParse<BorrowingStatus>(b.Status, true, out var parsedStatus);

                SelectedEquipmentHistory.Add(BorrowingRowItem.From(new BorrowingRecord
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

            ShowHistoryEmpty = SelectedEquipmentHistory.Count == 0;
        }
        catch (Exception ex)
        {
            SelectedEquipmentHistory.Clear();
            HistoryError = ex is ApiClientException ? ex.Message : DescribeException(ex);
        }
        finally
        {
            IsHistoryBusy = false;
        }
    }

    [RelayCommand]
    private void StartNewEquipment()
    {
        SelectedEquipment = null;
        PrepareNewEquipmentForm();
    }

    private void PrepareNewEquipmentForm()
    {
        FormHeader = "Add New Equipment";
        SaveButtonText = "Add Equipment";
        IdentifierInput = string.Empty;
        NameInput = string.Empty;
        DescriptionInput = string.Empty;
        AvailabilityStatusText = "Available (new item)";
    }

    [RelayCommand]
    private async Task SaveEquipmentAsync()
    {
        try
        {
            string? desc = string.IsNullOrWhiteSpace(DescriptionInput) ? null : DescriptionInput.Trim();
            string savedCode;

            if (SelectedEquipment is null)
            {
                var created = await _api.CreateEquipmentAsync(
                    IdentifierInput.Trim(),
                    NameInput.Trim(),
                    desc);
                savedCode = created.EquipmentCode;
            }
            else
            {
                var updated = await _api.UpdateEquipmentAsync(
                    SelectedEquipment.Id.ToString(CultureInfo.InvariantCulture),
                    IdentifierInput.Trim(),
                    NameInput.Trim(),
                    desc);
                savedCode = updated.EquipmentCode;
            }

            await LoadAsync();
            StatusMessage = HasError
                ? $"Equipment '{savedCode}' was saved, but the list could not be refreshed."
                : $"Equipment '{savedCode}' saved successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex is ApiClientException ? ex.Message : DescribeException(ex);
        }
    }
}