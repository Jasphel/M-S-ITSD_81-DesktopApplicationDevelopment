namespace EquipmentBorrowing.Application.Results;

public enum BorrowFailureReason
{
    None,
    StudentNotFound,
    StudentNotAllowedToBorrow,
    BorrowingLimitReached,
    EquipmentNotFound,
    EquipmentUnavailable
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

    private BorrowResult(bool succeeded, BorrowFailureReason failureReason)
    {
        Succeeded = succeeded;
        FailureReason = failureReason;
    }

    public static BorrowResult Success() =>
        new(true, BorrowFailureReason.None);

    public static BorrowResult Fail(BorrowFailureReason reason) =>
        new(false, reason);
}
