using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly EquipmentViewModel _equipmentViewModel;
    private readonly BorrowingsViewModel _borrowingsViewModel;
    private readonly StudentManagementViewModel _studentManagementViewModel;
    private readonly EquipmentManagementViewModel _equipmentManagementViewModel;

    [ObservableProperty]
    private ViewModelBase currentViewModel;

    public MainWindowViewModel(
        EquipmentViewModel equipmentViewModel,
        BorrowingsViewModel borrowingsViewModel,
        StudentManagementViewModel studentManagementViewModel,
        EquipmentManagementViewModel equipmentManagementViewModel)
    {
        _equipmentViewModel = equipmentViewModel;
        _borrowingsViewModel = borrowingsViewModel;
        _studentManagementViewModel = studentManagementViewModel;
        _equipmentManagementViewModel = equipmentManagementViewModel;

        currentViewModel = _equipmentViewModel;
        _ = ShowEquipmentCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task ShowEquipment()
    {
        await _equipmentViewModel.LoadAsync();
        CurrentViewModel = _equipmentViewModel;
    }

    [RelayCommand]
    private async Task ShowBorrowings()
    {
        await _borrowingsViewModel.LoadAsync();
        CurrentViewModel = _borrowingsViewModel;
    }

    [RelayCommand]
    private async Task ShowStudents()
    {
        await _studentManagementViewModel.LoadAsync();
        CurrentViewModel = _studentManagementViewModel;
    }

    [RelayCommand]
    private async Task ShowEquipmentManagement()
    {
        await _equipmentManagementViewModel.LoadAsync();
        CurrentViewModel = _equipmentManagementViewModel;
    }
}