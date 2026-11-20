namespace EquipmentBorrowing.Application.Results;

public enum BorrowFailureReason
{
    None,
    StudentNotFound,
    StudentNotAllowedToBorrow,
    BorrowingLimitReached,
    EquipmentNotFound,
    EquipmentUnavailable,
    InvalidReturnDate,
    NotEnoughItems,
    DuplicateItems,
    Conflict,
    PersistenceFailed,
    OutcomeUnknown
}

/// <summary>
/// The outcome of a borrow attempt. A plain bool cannot tell the caller
/// *why* an attempt failed, and Laboratory Activity 2 requires the
/// interface to show a specific, business-accurate reason (equipment
/// unavailable, limit reached, etc.) without recreating those rules in
/// the ViewModel. This type carries that reason out of the Application
/// layer instead.
/// </summary>
public class BorrowResult
{
    public bool Succeeded { get; }
    public BorrowFailureReason FailureReason { get; }

    /// <summary>
    /// For multi-item checkout: the equipment that caused the rejection
    /// (null when the rejection is about the student, the date or the request as a whole).
    /// </summary>
    public int? OffendingEquipmentId { get; }

    private BorrowResult(bool succeeded, BorrowFailureReason failureReason, int? offendingEquipmentId)
    {
        Succeeded = succeeded;
        FailureReason = failureReason;
        OffendingEquipmentId = offendingEquipmentId;
    }

    public static BorrowResult Success() =>
        new(true, BorrowFailureReason.None, null);

    public static BorrowResult Fail(BorrowFailureReason reason, int? offendingEquipmentId = null) =>
        new(false, reason, offendingEquipmentId);
}
