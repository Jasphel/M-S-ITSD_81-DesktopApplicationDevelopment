using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class EquipmentManagementViewModel : ViewModelBase
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly EquipmentManagementService _equipmentService;
    private readonly BorrowingQueryService _queryService;

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

    // Selected-item history (UC-08) has its own loading / error / empty state (BR-19).
    [ObservableProperty]
    private bool isHistoryBusy;

    [ObservableProperty]
    private string? historyError;

    [ObservableProperty]
    private bool showHistoryEmpty;

    public EquipmentManagementViewModel(
        IEquipmentRepository equipmentRepository,
        EquipmentManagementService equipmentService,
        BorrowingQueryService queryService)
    {
        _equipmentRepository = equipmentRepository;
        _equipmentService = equipmentService;
        _queryService = queryService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var list = await _equipmentRepository.GetAllAsync();

            EquipmentList.Clear();
            foreach (var item in list)
                EquipmentList.Add(item);

            PrepareNewEquipmentForm();
        }
        catch (Exception ex)
        {
            EquipmentList.Clear();
            ErrorMessage = DescribeException(ex);
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
            var records = await _queryService.GetEquipmentHistoryAsync(equipmentId);

            // Ignore a stale result if another item was selected while this was loading.
            if (SelectedEquipment?.Id != equipmentId)
                return;

            SelectedEquipmentHistory.Clear();
            foreach (var record in records)
                SelectedEquipmentHistory.Add(BorrowingRowItem.From(record));

            ShowHistoryEmpty = SelectedEquipmentHistory.Count == 0;
        }
        catch (Exception ex)
        {
            SelectedEquipmentHistory.Clear();
            HistoryError = DescribeException(ex);
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
            OperationResult result;
            if (SelectedEquipment is null)
            {
                // UC-15: Add Equipment
                result = await _equipmentService.AddEquipmentAsync(
                    IdentifierInput,
                    NameInput,
                    DescriptionInput);
            }
            else
            {
                // UC-16: Edit Equipment (availability is never edited here, BR-07)
                result = await _equipmentService.UpdateEquipmentAsync(
                    SelectedEquipment.Id,
                    IdentifierInput,
                    NameInput,
                    DescriptionInput);
            }

            if (!result.IsSuccess)
            {
                StatusMessage = result.ErrorMessage ?? "Operation failed.";
                return;
            }

            var savedIdentifier = Equipment.NormalizeIdentifier(IdentifierInput);
            await LoadAsync();
            StatusMessage = HasError
                ? $"Equipment '{savedIdentifier}' was saved, but the list could not be refreshed."
                : $"Equipment '{savedIdentifier}' saved successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = DescribeException(ex);
        }
    }
}
