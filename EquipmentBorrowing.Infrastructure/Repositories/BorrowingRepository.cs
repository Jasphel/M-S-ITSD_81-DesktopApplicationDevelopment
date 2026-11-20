using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class BorrowingRepository : IBorrowingRepository
{
    private readonly BorrowingDbContext _context;

    public BorrowingRepository(BorrowingDbContext context)
    {
        _context = context;
    }

    public async Task<Borrowing?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Borrowings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                borrowing => borrowing.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Borrowing>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Borrowings
            .AsNoTracking()
            .Where(borrowing => borrowing.Status == BorrowingStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveByStudentIdAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Borrowings
            .CountAsync(
                borrowing =>
                    borrowing.StudentId == studentId &&
                    borrowing.Status == BorrowingStatus.Active,
                cancellationToken);
    }

    public async Task<int> GetNextIdAsync(
        CancellationToken cancellationToken = default)
    {
        var maxId = await _context.Borrowings
            .Select(borrowing => (int?)borrowing.Id)
            .MaxAsync(cancellationToken);

        return (maxId ?? 0) + 1;
    }

    public async Task AddAsync(
        Borrowing borrowing,
        CancellationToken cancellationToken = default)
    {
        await _context.Borrowings.AddAsync(
            borrowing,
            cancellationToken);

        await DbSaveHelper.SaveAsync(_context, cancellationToken);
    }

    public async Task UpdateAsync(
    Borrowing borrowing,
    CancellationToken cancellationToken = default)
    {
        // Reads are untracked, so re-attach the edited entity and write it (last write wins).
        // A row that no longer exists surfaces as a DataConflictException.
        _context.Borrowings.Update(borrowing);
        await DbSaveHelper.SaveAsync(_context, cancellationToken);
    }
    public async Task<IReadOnlyList<Borrowing>> GetByStudentIdAsync(
           int studentId,
           CancellationToken cancellationToken = default)
    {
        return await _context.Borrowings
            .AsNoTracking()
            .Where(b => b.StudentId == studentId)
            .OrderByDescending(b => b.Id)
            .ToListAsync(cancellationToken);
    }
}