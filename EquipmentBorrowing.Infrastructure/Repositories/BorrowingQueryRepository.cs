using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Queries;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

/// <summary>
/// Read-only joined queries for histories (UC-08) and search (UC-09). All filtering and sorting
/// runs in PostgreSQL (BR-17); nothing is tracked, so results are always fresh.
/// </summary>
public class BorrowingQueryRepository : IBorrowingQueryRepository
{
    private readonly BorrowingDbContext _context;

    public BorrowingQueryRepository(BorrowingDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BorrowingRecord>> SearchAsync(
        BorrowingSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var query =
            from b in _context.Borrowings.AsNoTracking()
            join s in _context.Students on b.StudentId equals s.Id
            join e in _context.Equipment on b.EquipmentId equals e.Id
            select new { b, s, e };

        if (criteria.Status == BorrowingStatusFilter.Active)
        {
            query = query.Where(x => x.b.Status == BorrowingStatus.Active);
        }
        else if (criteria.Status == BorrowingStatusFilter.Returned)
        {
            query = query.Where(x => x.b.Status == BorrowingStatus.Returned);
        }

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            // Case-insensitive partial match on student name, student number or equipment name.
            var term = criteria.SearchText.Trim().ToLower();

            query = query.Where(x =>
                x.s.Name.ToLower().Contains(term) ||
                x.s.StudentNumber.ToLower().Contains(term) ||
                x.e.Name.ToLower().Contains(term));
        }

        // The borrowing id is always the tie-breaker so the order is stable.
        var ordered = (criteria.SortBy, criteria.Descending) switch
        {
            (BorrowingSortField.ExpectedReturnDate, true) =>
                query.OrderByDescending(x => x.b.ExpectedReturnDate).ThenByDescending(x => x.b.Id),
            (BorrowingSortField.ExpectedReturnDate, false) =>
                query.OrderBy(x => x.b.ExpectedReturnDate).ThenBy(x => x.b.Id),
            (_, true) =>
                query.OrderByDescending(x => x.b.DateBorrowed).ThenByDescending(x => x.b.Id),
            _ =>
                query.OrderBy(x => x.b.DateBorrowed).ThenBy(x => x.b.Id)
        };

        return await ordered
            .Select(x => new BorrowingRecord
            {
                BorrowingId = x.b.Id,
                StudentId = x.s.Id,
                StudentNumber = x.s.StudentNumber,
                StudentName = x.s.Name,
                EquipmentId = x.e.Id,
                EquipmentIdentifier = x.e.Identifier,
                EquipmentName = x.e.Name,
                DateBorrowed = x.b.DateBorrowed,
                ExpectedReturnDate = x.b.ExpectedReturnDate,
                ActualReturnDate = x.b.ActualReturnDate,
                Status = x.b.Status
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BorrowingRecord>> GetByEquipmentIdAsync(
        int equipmentId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from b in _context.Borrowings.AsNoTracking()
            where b.EquipmentId == equipmentId
            join s in _context.Students on b.StudentId equals s.Id
            join e in _context.Equipment on b.EquipmentId equals e.Id
            orderby b.Id descending
            select new BorrowingRecord
            {
                BorrowingId = b.Id,
                StudentId = s.Id,
                StudentNumber = s.StudentNumber,
                StudentName = s.Name,
                EquipmentId = e.Id,
                EquipmentIdentifier = e.Identifier,
                EquipmentName = e.Name,
                DateBorrowed = b.DateBorrowed,
                ExpectedReturnDate = b.ExpectedReturnDate,
                ActualReturnDate = b.ActualReturnDate,
                Status = b.Status
            })
            .ToListAsync(cancellationToken);
    }
}
