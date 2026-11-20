using EquipmentBorrowing.Application.Queries;

namespace EquipmentBorrowing.Application.Interfaces;

/// <summary>
/// Read side for histories and search. Kept separate from <see cref="IBorrowingRepository"/>
/// (the write side) because it returns joined, display-oriented rows instead of domain objects.
/// </summary>
public interface IBorrowingQueryRepository
{
    Task<IReadOnlyList<BorrowingRecord>> SearchAsync(
        BorrowingSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BorrowingRecord>> GetByEquipmentIdAsync(
        int equipmentId,
        CancellationToken cancellationToken = default);
}
