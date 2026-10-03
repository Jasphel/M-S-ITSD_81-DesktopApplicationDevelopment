using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IBorrowingRepository
{
    Task<Borrowing?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Borrowing>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<int> CountActiveByStudentIdAsync(
        int studentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserves the next identifier to use for a new borrowing. A real
    /// database repository would let the database auto-generate this
    /// (an IDENTITY / AUTOINCREMENT column); the in-memory repository
    /// has to do it itself instead.
    /// </summary>
    Task<int> GetNextIdAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Borrowing borrowing,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Borrowing borrowing,
        CancellationToken cancellationToken = default);
}