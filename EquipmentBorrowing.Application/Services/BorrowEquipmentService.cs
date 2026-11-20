using EquipmentBorrowing.Application.Exceptions;
using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Results;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

/// <summary>
/// UC-03 Borrow Equipment.
/// When an <see cref="IBorrowingTransactionFactory"/> is supplied (the desktop app),
/// the borrowing row and the "equipment is now unavailable" change are saved in one
/// PostgreSQL transaction. Without one (the Lab 1 console demo with in-memory
/// repositories) the same rules run directly against the injected repositories.
/// </summary>
public class BorrowEquipmentService
{
    public const int MaximumActiveBorrowings = 3;

    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IBorrowingRepository _borrowingRepository;
    private readonly IBorrowingTransactionFactory? _transactionFactory;

    public BorrowEquipmentService(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        IBorrowingRepository borrowingRepository,
        IBorrowingTransactionFactory? transactionFactory = null)
    {
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _borrowingRepository = borrowingRepository;
        _transactionFactory = transactionFactory;
    }

    public async Task<BorrowResult> BorrowEquipmentAsync(
        int borrowingId,
        int studentId,
        int equipmentId,
        DateTime expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        if (_transactionFactory is null)
        {
            return await BorrowCoreAsync(
                _studentRepository, _equipmentRepository, _borrowingRepository,
                borrowingId, studentId, equipmentId, expectedReturnDate, cancellationToken);
        }

        try
        {
            await using var transaction = await _transactionFactory.BeginAsync(cancellationToken);

            // Everything is (re)validated against current data inside the transaction (BR-15).
            var result = await BorrowCoreAsync(
                transaction.Students, transaction.Equipment, transaction.Borrowings,
                borrowingId, studentId, equipmentId, expectedReturnDate, cancellationToken);

            if (result.Succeeded)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return result;
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

    private static async Task<BorrowResult> BorrowCoreAsync(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        IBorrowingRepository borrowingRepository,
        int borrowingId,
        int studentId,
        int equipmentId,
        DateTime expectedReturnDate,
        CancellationToken cancellationToken)
    {
        // BR-11: the expected return date must be later than the checkout date (local calendar dates).
        if (expectedReturnDate.Date <= DateTime.Today)
        {
            return BorrowResult.Fail(BorrowFailureReason.InvalidReturnDate);
        }

        var student = await studentRepository.GetByIdAsync(studentId, cancellationToken);

        if (student is null)
        {
            return BorrowResult.Fail(BorrowFailureReason.StudentNotFound);
        }

        if (!student.IsAllowedToBorrow)
        {
            return BorrowResult.Fail(BorrowFailureReason.StudentNotAllowedToBorrow);
        }

        var activeBorrowings = await borrowingRepository.CountActiveByStudentIdAsync(
            studentId,
            cancellationToken);

        if (activeBorrowings >= MaximumActiveBorrowings)
        {
            return BorrowResult.Fail(BorrowFailureReason.BorrowingLimitReached);
        }

        var equipment = await equipmentRepository.GetByIdAsync(equipmentId, cancellationToken);

        if (equipment is null)
        {
            return BorrowResult.Fail(BorrowFailureReason.EquipmentNotFound);
        }

        if (!equipment.IsAvailable)
        {
            return BorrowResult.Fail(BorrowFailureReason.EquipmentUnavailable);
        }

        var borrowing = new Borrowing(
            borrowingId,
            studentId,
            equipmentId,
            DateTime.Now,
            expectedReturnDate);

        equipment.MarkAsBorrowed();

        await borrowingRepository.AddAsync(borrowing, cancellationToken);

        // The availability change must be written too, otherwise the item would
        // still look available even though it has an active borrowing.
        await equipmentRepository.UpdateAsync(equipment, cancellationToken);

        return BorrowResult.Success();
    }
}
