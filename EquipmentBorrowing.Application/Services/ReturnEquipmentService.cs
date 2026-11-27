using System;
using EquipmentBorrowing.Application.Exceptions;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Results;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

/// <summary>
/// UC-05 Return Equipment. Marks the borrowing returned and the equipment
/// available in one transaction when a transaction factory is supplied.
/// </summary>
public class ReturnEquipmentService
{
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IBorrowingTransactionFactory? _transactionFactory;

    public ReturnEquipmentService(
        IBorrowingRepository borrowingRepository,
        IEquipmentRepository equipmentRepository,
        IBorrowingTransactionFactory? transactionFactory = null)
    {
        _borrowingRepository = borrowingRepository;
        _equipmentRepository = equipmentRepository;
        _transactionFactory = transactionFactory;
    }

    public async Task<ReturnResult> ReturnEquipmentAsync(
        int borrowingId,
        CancellationToken cancellationToken = default)
    {
        if (_transactionFactory is null)
        {
            return await ReturnCoreAsync(
                _borrowingRepository, _equipmentRepository, borrowingId, cancellationToken);
        }

        try
        {
            await using var transaction = await _transactionFactory.BeginAsync(cancellationToken);

            var result = await ReturnCoreAsync(
                transaction.Borrowings, transaction.Equipment, borrowingId, cancellationToken);

            if (result.Succeeded)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return result;
        }
        catch (DataConflictException)
        {
            return ReturnResult.Fail(ReturnFailureReason.Conflict);
        }
        catch (CommitOutcomeUnknownException)
        {
            return ReturnResult.Fail(ReturnFailureReason.OutcomeUnknown);
        }
        catch (DataAccessException)
        {
            return ReturnResult.Fail(ReturnFailureReason.PersistenceFailed);
        }
    }

    private static async Task<ReturnResult> ReturnCoreAsync(
        IBorrowingRepository borrowingRepository,
        IEquipmentRepository equipmentRepository,
        int borrowingId,
        CancellationToken cancellationToken)
    {
        var borrowing = await borrowingRepository.GetByIdAsync(borrowingId, cancellationToken);

        if (borrowing is null)
        {
            return ReturnResult.Fail(ReturnFailureReason.BorrowingNotFound);
        }

        if (borrowing.Status == BorrowingStatus.Returned)
        {
            return ReturnResult.Fail(ReturnFailureReason.AlreadyReturned);
        }

        var equipment = await equipmentRepository.GetByIdAsync(borrowing.EquipmentId, cancellationToken);

        if (equipment is null)
        {
            return ReturnResult.Fail(ReturnFailureReason.EquipmentNotFound);
        }

        var utcReturnDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        borrowing.MarkAsReturned(utcReturnDate);
        equipment.MarkAsAvailable();

        await borrowingRepository.UpdateAsync(borrowing, cancellationToken);
        await equipmentRepository.UpdateAsync(equipment, cancellationToken);

        return ReturnResult.Success();
    }
}