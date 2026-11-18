using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Infrastructure.Repositories;

public class InMemoryEquipmentRepository : IEquipmentRepository
{
    private readonly List<Equipment> _equipment = new();

    public void Add(Equipment equipment) => _equipment.Add(equipment);

    public Task<Equipment?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_equipment.FirstOrDefault(e => e.Id == id));

    public Task<Equipment?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var normalized = Equipment.NormalizeIdentifier(identifier);
        return Task.FromResult(_equipment.FirstOrDefault(e => e.Identifier == normalized));
    }

    public Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Equipment>>(_equipment.ToList());

    public Task AddAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        _equipment.Add(equipment);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Equipment equipment, CancellationToken cancellationToken = default)
    {
        var index = _equipment.FindIndex(e => e.Id == equipment.Id);
        if (index >= 0)
            _equipment[index] = equipment;
        return Task.CompletedTask;
    }
}