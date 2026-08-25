using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Interfaces;

public interface IEquipmentRepository
{
    Task<Equipment?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
}