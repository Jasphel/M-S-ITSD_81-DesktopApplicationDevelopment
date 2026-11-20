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

    [ObservableProperty]
    private bool isStarting = true;

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
    }

    /// <summary>
    /// Runs the supplied database bootstrap (migrations + safe seed) in the background, then opens the
    /// equipment screen. A failed bootstrap is logged and never thrown: each screen reports its own
    /// connection problem and offers Retry.
    /// </summary>
    public async Task StartAsync(Func<Task> initializeDatabase)
    {
        try
        {
            await initializeDatabase();
        }
        catch (Exception ex)
        {
            ErrorLog.Write(ex);
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
}