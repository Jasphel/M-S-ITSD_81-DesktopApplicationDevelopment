using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;


namespace EquipmentBorrowing.Application.Services;

public class EquipmentManagementService
{
    private readonly IEquipmentRepository _equipmentRepository;

    public EquipmentManagementService(IEquipmentRepository equipmentRepository)
    {
        _equipmentRepository = equipmentRepository;
    }

    // UC-15: Add Equipment
    public async Task<OperationResult> AddEquipmentAsync(
        string identifier,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return OperationResult.Fail("Equipment identifier / asset tag is required.");
        if (string.IsNullOrWhiteSpace(name))
            return OperationResult.Fail("Equipment name is required.");

        var existing = await _equipmentRepository.GetByIdentifierAsync(identifier, cancellationToken);
        if (existing is not null)
            return OperationResult.Fail($"Identifier '{Equipment.NormalizeIdentifier(identifier)}' already exists.");

        try
        {
            var equipment = new Equipment(0, identifier, name, description);
            await _equipmentRepository.AddAsync(equipment, cancellationToken);
            return OperationResult.Success();
        }
        catch (ArgumentException ex)
        {
            return OperationResult.Fail(ex.Message);
        }
    }

    // UC-16: Edit Equipment Details (preserves IsAvailable status)
    public async Task<OperationResult> UpdateEquipmentAsync(
        int equipmentId,
        string identifier,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);
        if (equipment is null)
            return OperationResult.Fail("Selected equipment was not found.");

        if (string.IsNullOrWhiteSpace(identifier))
            return OperationResult.Fail("Equipment identifier / asset tag is required.");
        if (string.IsNullOrWhiteSpace(name))
            return OperationResult.Fail("Equipment name is required.");

        var duplicate = await _equipmentRepository.GetByIdentifierAsync(identifier, cancellationToken);
        if (duplicate is not null && duplicate.Id != equipmentId)
            return OperationResult.Fail($"Another item already uses identifier '{Equipment.NormalizeIdentifier(identifier)}'.");

        try
        {
            equipment.UpdateDetails(identifier, name, description);
            await _equipmentRepository.UpdateAsync(equipment, cancellationToken);
            return OperationResult.Success();
        }
        catch (ArgumentException ex)
        {
            return OperationResult.Fail(ex.Message);
        }
    }
}