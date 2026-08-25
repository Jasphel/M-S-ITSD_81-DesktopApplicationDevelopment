using EquipmentBorrowing.Application.Interfaces;
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

    public async Task<bool> BorrowEquipmentAsync(
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
            return false;
        }

        if (!student.IsAllowedToBorrow)
        {
            return false;
        }

        const int maximumActiveBorrowings = 3;

        var activeBorrowings =
            await _borrowingRepository.CountActiveByStudentIdAsync(
                studentId,
                cancellationToken);

        if (activeBorrowings >= maximumActiveBorrowings)
        {
            return false;
        }

        var equipment = await _equipmentRepository.GetByIdAsync(
            equipmentId,
            cancellationToken);

        if (equipment is null)
        {
            return false;
        }

        if (!equipment.IsAvailable)
        {
            return false;
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

        return true;
    }
}