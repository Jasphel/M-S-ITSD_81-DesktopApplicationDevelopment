using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryBorrowingRepository : IBorrowingRepository
{
    private readonly List<Borrowing> _borrowings = new();

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

    public Task AddAsync(
        Borrowing borrowing,
        CancellationToken cancellationToken = default)
    {
        _borrowings.Add(borrowing);

        return Task.CompletedTask;
    }
}