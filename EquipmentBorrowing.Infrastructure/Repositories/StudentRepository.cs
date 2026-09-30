using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly BorrowingDbContext _context;

    public StudentRepository(BorrowingDbContext context)
    {
        _context = context;
    }

    public async Task<Student?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .FirstOrDefaultAsync(
                student => student.Id == id,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Student student,
        CancellationToken cancellationToken = default)
    {
        await _context.Students.AddAsync(student, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}