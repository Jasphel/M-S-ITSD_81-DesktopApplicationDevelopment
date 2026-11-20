using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class EquipmentRepository : IEquipmentRepository
{
    private readonly BorrowingDbContext _context;

    public EquipmentRepository(BorrowingDbContext context)
    {
        _context = context;
    }

    public async Task<Equipment?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Equipment
            .AsNoTracking()
            .FirstOrDefaultAsync(
                equipment => equipment.Id == id,
                cancellationToken);
    }

    public async Task<Equipment?> GetByIdentifierAsync(
        string identifier,
        CancellationToken cancellationToken = default)
    {
        var normalized = Equipment.NormalizeIdentifier(identifier);
        return await _context.Equipment
            .AsNoTracking()
            .FirstOrDefaultAsync(
                equipment => equipment.Identifier == normalized,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Equipment>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Equipment
            .AsNoTracking()
            .OrderBy(equipment => equipment.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Equipment equipment,
        CancellationToken cancellationToken = default)
    {
        await _context.Equipment.AddAsync(equipment, cancellationToken);
        await DbSaveHelper.SaveAsync(_context, cancellationToken);
    }

    public async Task UpdateAsync(
        Equipment equipment,
        CancellationToken cancellationToken = default)
    {
        // Reads are untracked, so re-attach the edited entity and write it (last write wins).
        // A row that no longer exists surfaces as a DataConflictException.
        _context.Equipment.Update(equipment);
        await DbSaveHelper.SaveAsync(_context, cancellationToken);
    }
}