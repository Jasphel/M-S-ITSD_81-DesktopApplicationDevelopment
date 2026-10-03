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
            .FirstOrDefaultAsync(
                equipment => equipment.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Equipment>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Equipment
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(
    Equipment equipment,
    CancellationToken cancellationToken = default)
    {
        var existing = await _context.Equipment.FindAsync(new object[] { equipment.Id }, cancellationToken);
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(equipment);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}