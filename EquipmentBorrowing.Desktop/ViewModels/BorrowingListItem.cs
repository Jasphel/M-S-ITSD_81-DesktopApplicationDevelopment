namespace EquipmentBorrowing.Desktop.ViewModels;

/// <summary>
/// Borrowing (in Domain) only stores StudentId/EquipmentId, which is
/// correct for a domain object — it shouldn't need to know about names
/// just to enforce rules. This type exists purely so the Active
/// Borrowings screen has something readable to bind to; it carries no
/// behavior and enforces no rules, so it stays out of Domain/Application.
/// </summary>
public class BorrowingListItem
{
    public required int Id { get; init; }
    public required string StudentName { get; init; }
    public required string EquipmentName { get; init; }
    public required DateTime DateBorrowed { get; init; }
    public required DateTime ExpectedReturnDate { get; init; }
}
