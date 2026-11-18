using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<Student?> GetByStudentNumberAsync(
        string studentNumber,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Student>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Student student,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Student student,
        CancellationToken cancellationToken = default);
}