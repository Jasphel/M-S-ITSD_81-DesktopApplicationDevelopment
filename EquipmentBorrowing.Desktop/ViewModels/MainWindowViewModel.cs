using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly EquipmentViewModel _equipmentViewModel;
    private readonly BorrowingsViewModel _borrowingsViewModel;
    private readonly StudentManagementViewModel _studentManagementViewModel;
    private readonly EquipmentManagementViewModel _equipmentManagementViewModel;
    private readonly BorrowingSearchViewModel _borrowingSearchViewModel;

    [ObservableProperty]
    private ViewModelBase currentViewModel;

    [ObservableProperty]
    private bool isStarting = true;

    public MainWindowViewModel(
        EquipmentViewModel equipmentViewModel,
        BorrowingsViewModel borrowingsViewModel,
        StudentManagementViewModel studentManagementViewModel,
        EquipmentManagementViewModel equipmentManagementViewModel,
        BorrowingSearchViewModel borrowingSearchViewModel)
    {
        _equipmentViewModel = equipmentViewModel;
        _borrowingsViewModel = borrowingsViewModel;
        _studentManagementViewModel = studentManagementViewModel;
        _equipmentManagementViewModel = equipmentManagementViewModel;
        _borrowingSearchViewModel = borrowingSearchViewModel;

        currentViewModel = _equipmentViewModel;
    }

    public async Task StartAsync(Func<Task>? initializeHook = null)
    {
        if (initializeHook is not null)
        {
            try
            {
                await initializeHook();
            }
            catch (Exception ex)
            {
                ErrorLog.Write(ex);
            }
        }

        IsStarting = false;
        await ShowEquipment();
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

    [RelayCommand]
    private async Task ShowSearch()
    {
        await _borrowingSearchViewModel.LoadAsync();
        CurrentViewModel = _borrowingSearchViewModel;
    }
}