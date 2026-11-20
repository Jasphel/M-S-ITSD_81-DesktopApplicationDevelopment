namespace EquipmentBorrowing.Application.Interfaces;

/// <summary>
/// One database transaction that spans the student, equipment and borrowing
/// repositories. All three share the same underlying unit of work, so an
/// issue/return changes the borrowing row and the equipment availability
/// together or not at all (BR-10, BR-15).
/// Disposing without calling <see cref="CommitAsync"/> rolls everything back.
/// </summary>
public interface IBorrowingTransaction : IAsyncDisposable
{
    IStudentRepository Students { get; }
    IEquipmentRepository Equipment { get; }
    IBorrowingRepository Borrowings { get; }

    Task CommitAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Starts a new <see cref="IBorrowingTransaction"/>. The context lifetime is
/// "one transaction = one short-lived context", which is safe for async code.
/// </summary>
public interface IBorrowingTransactionFactory
{
    Task<IBorrowingTransaction> BeginAsync(CancellationToken cancellationToken = default);
}
