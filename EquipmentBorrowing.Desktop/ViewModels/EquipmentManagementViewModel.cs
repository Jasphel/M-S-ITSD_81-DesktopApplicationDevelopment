using System.Collections.ObjectModel;
using Avalonia.Threading;
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

    public ObservableCollection<Equipment> EquipmentList { get; } = new();

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

    public EquipmentManagementViewModel(
        IEquipmentRepository equipmentRepository,
        EquipmentManagementService equipmentService)
    {
        _equipmentRepository = equipmentRepository;
        _equipmentService = equipmentService;
    }

    public async Task LoadAsync()
    {
        var list = await _equipmentRepository.GetAllAsync();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            EquipmentList.Clear();
            foreach (var item in list)
                EquipmentList.Add(item);

            PrepareNewEquipmentForm();
        });
    }

    partial void OnSelectedEquipmentChanged(Equipment? value)
    {
        if (value is null)
        {
            PrepareNewEquipmentForm();
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
            // UC-16: Edit Equipment
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
        StatusMessage = $"Equipment '{savedIdentifier}' saved successfully.";
    }
}