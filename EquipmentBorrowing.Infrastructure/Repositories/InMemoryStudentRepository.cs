using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryStudentRepository : IStudentRepository
{
    private readonly List<Student> _students = new();

    public Task<Student?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var student = _students.FirstOrDefault(
            student => student.Id == id);

        return Task.FromResult(student);
    }

    public Task<IReadOnlyList<Student>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Student>>(_students.ToList());
    }

    public void Add(Student student)
    {
        _students.Add(student);
    }
}