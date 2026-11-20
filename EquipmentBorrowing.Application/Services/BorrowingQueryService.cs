using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Queries;

namespace EquipmentBorrowing.Application.Services;

/// <summary>UC-08 equipment history and UC-09 search / filter / sort.</summary>
public class BorrowingQueryService
{
    private readonly IBorrowingQueryRepository _queryRepository;

    public BorrowingQueryService(IBorrowingQueryRepository queryRepository)
    {
        _queryRepository = queryRepository;
    }

    public Task<IReadOnlyList<BorrowingRecord>> SearchAsync(
        BorrowingSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        // BR-17: trim surrounding whitespace; blank text means "no text filter".
        var text = string.IsNullOrWhiteSpace(criteria.SearchText) ? null : criteria.SearchText.Trim();

        var normalized = new BorrowingSearchCriteria
        {
            SearchText = text,
            Status = criteria.Status,
            SortBy = criteria.SortBy,
            Descending = criteria.Descending
        };

        return _queryRepository.SearchAsync(normalized, cancellationToken);
    }

    public Task<IReadOnlyList<BorrowingRecord>> GetEquipmentHistoryAsync(
        int equipmentId,
        CancellationToken cancellationToken = default)
    {
        return _queryRepository.GetByEquipmentIdAsync(equipmentId, cancellationToken);
    }
}
