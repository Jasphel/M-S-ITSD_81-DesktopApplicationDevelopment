using EquipmentBorrowing.Application.Exceptions;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Results;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

/// <summary>
/// UC-06 Check Out Multiple Equipment Items.
/// One student, one expected return date, at least two distinct items. Every borrowing row and
/// every availability change is written inside ONE database transaction: either all of them are
/// committed or none are (BR-12, BR-15). Eligibility, the active-item limit, references and
/// availability are re-checked against current data inside that transaction.
/// </summary>
public class CheckoutMultipleEquipmentService
{
    private readonly IBorrowingTransactionFactory _transactionFactory;

    public CheckoutMultipleEquipmentService(IBorrowingTransactionFactory transactionFactory)
    {
        _transactionFactory = transactionFactory;
    }

    public async Task<BorrowResult> CheckOutAsync(
        int studentId,
        IReadOnlyCollection<int> equipmentIds,
        DateTime expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        // --- Shape of the request (BR-12, BR-11): rejected without touching the database ---
        if (equipmentIds.Count < 2)
        {
            return BorrowResult.Fail(BorrowFailureReason.NotEnoughItems);
        }

        if (equipmentIds.Distinct().Count() != equipmentIds.Count)
        {
            return BorrowResult.Fail(BorrowFailureReason.DuplicateItems);
        }

        if (expectedReturnDate.Date <= DateTime.Today)
        {
            return BorrowResult.Fail(BorrowFailureReason.InvalidReturnDate);
        }

        try
        {
            await using var transaction = await _transactionFactory.BeginAsync(cancellationToken);

            // --- Revalidate against current data at the transaction boundary (BR-15) ---
            var student = await transaction.Students.GetByIdAsync(studentId, cancellationToken);
            if (student is null)
            {
                return BorrowResult.Fail(BorrowFailureReason.StudentNotFound);
            }

            if (!student.IsAllowedToBorrow)
            {
                return BorrowResult.Fail(BorrowFailureReason.StudentNotAllowedToBorrow);
            }

            // BR-09: existing active records + all requested items must not exceed the limit.
            var activeCount = await transaction.Borrowings.CountActiveByStudentIdAsync(studentId, cancellationToken);
            if (activeCount + equipmentIds.Count > BorrowEquipmentService.MaximumActiveBorrowings)
            {
                return BorrowResult.Fail(BorrowFailureReason.BorrowingLimitReached);
            }

            var items = new List<Equipment>();
            foreach (var equipmentId in equipmentIds)
            {
                var equipment = await transaction.Equipment.GetByIdAsync(equipmentId, cancellationToken);

                if (equipment is null)
                {
                    return BorrowResult.Fail(BorrowFailureReason.EquipmentNotFound, equipmentId);
                }

                if (!equipment.IsAvailable)
                {
                    return BorrowResult.Fail(BorrowFailureReason.EquipmentUnavailable, equipmentId);
                }

                items.Add(equipment);
            }

            // --- Writes: one active borrowing per item + the availability change ---
            var checkoutTime = DateTime.UtcNow;
            var utcReturnDate = ToUtcDateTime(expectedReturnDate);

            foreach (var equipment in items)
            {
                equipment.MarkAsBorrowed();

                await transaction.Borrowings.AddAsync(
                    new Borrowing(0, studentId, equipment.Id, checkoutTime, utcReturnDate),
                    cancellationToken);

                await transaction.Equipment.UpdateAsync(equipment, cancellationToken);
            }

            // Nothing is visible to anyone else until this succeeds. If it throws, or if any write
            // above failed, disposing the transaction rolls every row back.
            await transaction.CommitAsync(cancellationToken);

            return BorrowResult.Success();
        }
        catch (DataConflictException)
        {
            return BorrowResult.Fail(BorrowFailureReason.Conflict);
        }
        catch (CommitOutcomeUnknownException)
        {
            return BorrowResult.Fail(BorrowFailureReason.OutcomeUnknown);
        }
        catch (DataAccessException)
        {
            return BorrowResult.Fail(BorrowFailureReason.PersistenceFailed);
        }
    }

    private static DateTime ToUtcDateTime(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
    };
}