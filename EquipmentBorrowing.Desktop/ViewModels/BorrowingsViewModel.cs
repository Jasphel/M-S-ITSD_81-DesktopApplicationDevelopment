using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Results;
using EquipmentBorrowing.Application.Services;

namespace EquipmentBorrowing.Desktop.ViewModels;

public partial class BorrowingsViewModel : ViewModelBase
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ReturnEquipmentService _returnEquipmentService;

    public ObservableCollection<BorrowingListItem> ActiveBorrowings { get; } = new();

    [ObservableProperty]
    private BorrowingListItem? selectedBorrowing;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool showEmptyState;

    public BorrowingsViewModel(
        IBorrowingRepository borrowingRepository,
        IEquipmentRepository equipmentRepository,
        IStudentRepository studentRepository,
        ReturnEquipmentService returnEquipmentService)
    {
        _borrowingRepository = borrowingRepository;
        _equipmentRepository = equipmentRepository;
        _studentRepository = studentRepository;
        _returnEquipmentService = returnEquipmentService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var active = await _borrowingRepository.GetActiveAsync();
            var students = (await _studentRepository.GetAllAsync()).ToDictionary(s => s.Id);
            var equipment = (await _equipmentRepository.GetAllAsync()).ToDictionary(e => e.Id);

            ActiveBorrowings.Clear();

            foreach (var borrowing in active)
            {
                var studentName = students.TryGetValue(borrowing.StudentId, out var student)
                    ? student.Name
                    : "Unknown student";

                var equipmentName = equipment.TryGetValue(borrowing.EquipmentId, out var item)
                    ? item.Name
                    : "Unknown equipment";

                ActiveBorrowings.Add(new BorrowingListItem
                {
                    Id = borrowing.Id,
                    StudentName = studentName,
                    EquipmentName = equipmentName,
                    DateBorrowed = borrowing.DateBorrowed,
                    ExpectedReturnDate = borrowing.ExpectedReturnDate
                });
            }
        }
        catch (Exception ex)
        {
            ActiveBorrowings.Clear();
            ErrorMessage = DescribeException(ex);
        }
        finally
        {
            IsBusy = false;
            ShowEmptyState = !HasError && ActiveBorrowings.Count == 0;
        }
    }

    [RelayCommand]
    private async Task ReturnAsync()
    {
        if (SelectedBorrowing is null)
        {
            StatusMessage = "Please select a borrowing to return.";
            return;
        }

        var selected = SelectedBorrowing;

        try
        {
            var result = await _returnEquipmentService.ReturnEquipmentAsync(selected.Id);

            StatusMessage = result.Succeeded
                ? $"Returned \"{selected.EquipmentName}\"."
                : DescribeFailure(result.FailureReason);

            SelectedBorrowing = null;
            await LoadAsync(); // refresh in every case so a stale selection disappears
        }
        catch (Exception ex)
        {
            StatusMessage = DescribeException(ex);
        }
    }

    private static string DescribeFailure(ReturnFailureReason reason) => reason switch
    {
        ReturnFailureReason.BorrowingNotFound => "That borrowing record could not be found.",
        ReturnFailureReason.AlreadyReturned => "This equipment has already been returned. No change was made.",
        ReturnFailureReason.EquipmentNotFound => "The related equipment record could not be found.",
        ReturnFailureReason.Conflict => "The record changed while saving. Nothing was saved and the list was refreshed.",
        ReturnFailureReason.PersistenceFailed => "The return could not be saved. Nothing was changed.",
        ReturnFailureReason.OutcomeUnknown => "The connection was lost while saving, so the result is unknown. Refresh and check the list before trying again.",
        _ => "The return request could not be completed."
    };
}
