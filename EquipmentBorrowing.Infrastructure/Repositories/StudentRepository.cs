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
            .AsNoTracking()
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
            .AsNoTracking()
            .FirstOrDefaultAsync(
                student => student.StudentNumber == normalized,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Student>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .AsNoTracking()
            .OrderBy(student => student.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Student student,
        CancellationToken cancellationToken = default)
    {
        await _context.Students.AddAsync(student, cancellationToken);
        await DbSaveHelper.SaveAsync(_context, cancellationToken);
    }

    public async Task UpdateAsync(
        Student student,
        CancellationToken cancellationToken = default)
    {
        // Reads are untracked, so re-attach the edited entity and write it (last write wins).
        // A row that no longer exists surfaces as a DataConflictException.
        _context.Students.Update(student);
        await DbSaveHelper.SaveAsync(_context, cancellationToken);
    }
}