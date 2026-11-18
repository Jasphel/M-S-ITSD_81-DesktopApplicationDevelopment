using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryStudentRepository : IStudentRepository
{
    private readonly List<Student> _students = new();

    public void Add(Student student) => _students.Add(student);

    public Task<Student?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_students.FirstOrDefault(s => s.Id == id));

    public Task<Student?> GetByStudentNumberAsync(string studentNumber, CancellationToken cancellationToken = default)
    {
        var normalized = Student.NormalizeNumber(studentNumber);
        return Task.FromResult(_students.FirstOrDefault(s => s.StudentNumber == normalized));
    }

    public Task<IReadOnlyList<Student>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Student>>(_students.ToList());

    public Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        _students.Add(student);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        var index = _students.FindIndex(s => s.Id == student.Id);
        if (index >= 0)
            _students[index] = student;
        return Task.CompletedTask;
    }
}