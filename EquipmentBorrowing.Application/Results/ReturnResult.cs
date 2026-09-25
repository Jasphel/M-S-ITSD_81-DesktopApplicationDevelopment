namespace EquipmentBorrowing.Application.Results;

public enum ReturnFailureReason
{
    None,
    BorrowingNotFound,
    AlreadyReturned,
    EquipmentNotFound
}

public class ReturnResult
{
    public bool Succeeded { get; }
    public ReturnFailureReason FailureReason { get; }

    private ReturnResult(bool succeeded, ReturnFailureReason failureReason)
    {
        Succeeded = succeeded;
        FailureReason = failureReason;
    }

    public static ReturnResult Success() =>
        new(true, ReturnFailureReason.None);

    public static ReturnResult Fail(ReturnFailureReason reason) =>
        new(false, reason);
}
