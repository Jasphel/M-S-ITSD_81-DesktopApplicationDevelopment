using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Results;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class ReturnEquipmentService
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IEquipmentRepository _equipmentRepository;

    public ReturnEquipmentService(
        IBorrowingRepository borrowingRepository,
        IEquipmentRepository equipmentRepository)
    {
        _borrowingRepository = borrowingRepository;
        _equipmentRepository = equipmentRepository;
    }

    public async Task<ReturnResult> ReturnEquipmentAsync(
        int borrowingId,
        CancellationToken cancellationToken = default)
    {
        var borrowing = await _borrowingRepository.GetByIdAsync(
            borrowingId,
            cancellationToken);

        if (borrowing is null)
        {
            return ReturnResult.Fail(ReturnFailureReason.BorrowingNotFound);
        }

        if (borrowing.Status == BorrowingStatus.Returned)
        {
            return ReturnResult.Fail(ReturnFailureReason.AlreadyReturned);
        }

        var equipment = await _equipmentRepository.GetByIdAsync(
            borrowing.EquipmentId,
            cancellationToken);

        if (equipment is null)
        {
            return ReturnResult.Fail(ReturnFailureReason.EquipmentNotFound);
        }

        borrowing.MarkAsReturned();
        equipment.MarkAsAvailable();

        return ReturnResult.Success();
    }
}
