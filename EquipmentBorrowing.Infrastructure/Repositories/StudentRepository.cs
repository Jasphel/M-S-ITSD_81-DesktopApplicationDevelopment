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

    public async Task<Student?> GetByStudentNumberAsync(
        string studentNumber,
        CancellationToken cancellationToken = default)
    {
        var normalized = Student.NormalizeNumber(studentNumber);
        return await _context.Students
            .FirstOrDefaultAsync(
                student => student.StudentNumber == normalized,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .OrderBy(student => student.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Student student,
        CancellationToken cancellationToken = default)
    {
        await _context.Students.AddAsync(student, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Student student,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Students.FindAsync(new object[] { student.Id }, cancellationToken);
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(student);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}