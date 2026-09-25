using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Application.Results;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public class BorrowEquipmentService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IBorrowingRepository _borrowingRepository;

    public BorrowEquipmentService(
        IStudentRepository studentRepository,
        IEquipmentRepository equipmentRepository,
        IBorrowingRepository borrowingRepository)
    {
        _studentRepository = studentRepository;
        _equipmentRepository = equipmentRepository;
        _borrowingRepository = borrowingRepository;
    }

    public async Task<BorrowResult> BorrowEquipmentAsync(
        int borrowingId,
        int studentId,
        int equipmentId,
        DateTime expectedReturnDate,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(
            studentId,
            cancellationToken);

        if (student is null)
        {
            return BorrowResult.Fail(BorrowFailureReason.StudentNotFound);
        }

        if (!student.IsAllowedToBorrow)
        {
            return BorrowResult.Fail(BorrowFailureReason.StudentNotAllowedToBorrow);
        }

        const int maximumActiveBorrowings = 3;

        var activeBorrowings =
            await _borrowingRepository.CountActiveByStudentIdAsync(
                studentId,
                cancellationToken);

        if (activeBorrowings >= maximumActiveBorrowings)
        {
            return BorrowResult.Fail(BorrowFailureReason.BorrowingLimitReached);
        }

        var equipment = await _equipmentRepository.GetByIdAsync(
            equipmentId,
            cancellationToken);

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

        await _borrowingRepository.AddAsync(
            borrowing,
            cancellationToken);

        return BorrowResult.Success();
    }
}
