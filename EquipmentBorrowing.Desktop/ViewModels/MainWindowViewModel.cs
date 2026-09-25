using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EquipmentBorrowing.Desktop.ViewModels;

/// <summary>
/// This is the only ViewModel that knows both other ViewModels exist.
/// It doesn't contain any borrowing logic itself — it just decides
/// *which* screen is currently visible, and makes sure that screen's
/// data is fresh before showing it. That last part matters: a return
/// completed on the Borrowings screen changes equipment availability,
/// so Equipment needs to reload the next time someone switches to it,
/// and vice versa.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly EquipmentViewModel _equipmentViewModel;
    private readonly BorrowingsViewModel _borrowingsViewModel;

    [ObservableProperty]
    private ViewModelBase currentViewModel;

    public MainWindowViewModel(
        EquipmentViewModel equipmentViewModel,
        BorrowingsViewModel borrowingsViewModel)
    {
        _equipmentViewModel = equipmentViewModel;
        _borrowingsViewModel = borrowingsViewModel;

        currentViewModel = _equipmentViewModel;

        // Fire the initial load without blocking the constructor.
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
}
