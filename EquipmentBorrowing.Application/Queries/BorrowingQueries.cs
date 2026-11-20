using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Queries;

public enum BorrowingStatusFilter
{
    All,
    Active,
    Returned
}

public enum BorrowingSortField
{
    CheckoutDate,
    ExpectedReturnDate
}

/// <summary>BR-17: text search + status filter + sort. Every part is optional and can be cleared.</summary>
public sealed class BorrowingSearchCriteria
{
    public string? SearchText { get; init; }
    public BorrowingStatusFilter Status { get; init; } = BorrowingStatusFilter.All;
    public BorrowingSortField SortBy { get; init; } = BorrowingSortField.CheckoutDate;
    public bool Descending { get; init; } = true;
}

/// <summary>
/// A borrowing joined with the student and equipment it refers to. Read-only; used by the
/// history (UC-07, UC-08) and search (UC-09) screens.
/// </summary>
public sealed class BorrowingRecord
{
    public required int BorrowingId { get; init; }
    public required int StudentId { get; init; }
    public required string StudentNumber { get; init; }
    public required string StudentName { get; init; }
    public required int EquipmentId { get; init; }
    public required string EquipmentIdentifier { get; init; }
    public required string EquipmentName { get; init; }
    public required DateTime DateBorrowed { get; init; }
    public required DateTime ExpectedReturnDate { get; init; }
    public DateTime? ActualReturnDate { get; init; }
    public required BorrowingStatus Status { get; init; }
}
