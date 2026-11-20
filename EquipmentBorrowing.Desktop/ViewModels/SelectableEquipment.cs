using CommunityToolkit.Mvvm.ComponentModel;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

/// <summary>
/// One row of the equipment catalog plus a tick box. Ticking one item = UC-03 (single borrow);
/// ticking two or more = UC-06 (coordinated checkout). The wrapper only holds presentation state;
/// the domain Equipment is untouched.
/// </summary>
public partial class SelectableEquipment : ObservableObject
{
    public SelectableEquipment(Equipment item)
    {
        Item = item;
    }

    public Equipment Item { get; }

    public int Id => Item.Id;
    public string Identifier => Item.Identifier;
    public string Name => Item.Name;
    public bool IsAvailable => Item.IsAvailable;

    [ObservableProperty]
    private bool isChecked;
}
