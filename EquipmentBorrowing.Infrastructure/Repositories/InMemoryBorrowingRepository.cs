using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryBorrowingRepository : IBorrowingRepository
{
    private readonly List<Borrowing> _borrowings = new();
    private int _nextId = 1;

    public Task<int> GetNextIdAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_nextId++);
    }

    public Task<Borrowing?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var borrowing = _borrowings.FirstOrDefault(
            borrowing => borrowing.Id == id);

        return Task.FromResult(borrowing);
    }

    public Task<IReadOnlyList<Borrowing>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var active = _borrowings
            .Where(borrowing => borrowing.Status == BorrowingStatus.Active)
            .ToList();

        return Task.FromResult<IReadOnlyList<Borrowing>>(active);
    }

    public Task<int> CountActiveByStudentIdAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var count = _borrowings.Count(
            borrowing =>
                borrowing.StudentId == studentId &&
                borrowing.Status == BorrowingStatus.Active);

        return Task.FromResult(count);
    }

    public Task UpdateAsync(
    Borrowing borrowing,
    CancellationToken cancellationToken = default)
    {
        // In-memory object is already updated by reference
        return Task.CompletedTask;
    }

    public Task AddAsync(
        Borrowing borrowing,
        CancellationToken cancellationToken = default)
    {
        _borrowings.Add(borrowing);

        return Task.CompletedTask;
    }
}