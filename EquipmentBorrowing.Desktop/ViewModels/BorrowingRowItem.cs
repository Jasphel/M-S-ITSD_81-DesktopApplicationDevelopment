using EquipmentBorrowing.Application.Queries;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Desktop.ViewModels;

/// <summary>Display-ready borrowing row used by the equipment history (UC-08) and search (UC-09) screens.</summary>
public sealed record BorrowingRowItem(
    int BorrowingId,
    string StudentText,
    string EquipmentText,
    string BorrowedText,
    string DueText,
    string ReturnedText,
    string StatusText,
    bool IsReturned)
{
    public static BorrowingRowItem From(BorrowingRecord record) => new(
        record.BorrowingId,
        $"{record.StudentName} ({record.StudentNumber})",
        $"{record.EquipmentIdentifier} - {record.EquipmentName}",
        record.DateBorrowed.ToString("yyyy-MM-dd"),
        record.ExpectedReturnDate.ToString("yyyy-MM-dd"),
        record.ActualReturnDate?.ToString("yyyy-MM-dd") ?? "—",
        record.Status.ToString(),
        record.Status == BorrowingStatus.Returned);
}
