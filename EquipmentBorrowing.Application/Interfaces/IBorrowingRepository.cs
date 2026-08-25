using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IBorrowingRepository
{
    Task<int> CountActiveByStudentIdAsync(
        int studentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Borrowing borrowing,
        CancellationToken cancellationToken = default);
}