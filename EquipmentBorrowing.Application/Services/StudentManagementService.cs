using EquipmentBorrowing.Application.Interfaces;
using EquipmentBorrowing.Domain;

namespace EquipmentBorrowing.Application.Services;

public sealed record OperationResult(bool IsSuccess, string? ErrorMessage = null)
{
    public static OperationResult Success() => new(true);
    public static OperationResult Fail(string message) => new(false, message);
}

public class StudentManagementService
{
    private readonly IStudentRepository _studentRepository;
    private readonly IBorrowingRepository _borrowingRepository;

    public StudentManagementService(
        IStudentRepository studentRepository,
        IBorrowingRepository borrowingRepository)
    {
        _studentRepository = studentRepository;
        _borrowingRepository = borrowingRepository;
    }

    // UC-12: Register Student
    public async Task<OperationResult> RegisterStudentAsync(
        string studentNumber,
        string name,
        string? contactInfo,
        bool isAllowedToBorrow = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(studentNumber))
            return OperationResult.Fail("Student number is required.");
        if (string.IsNullOrWhiteSpace(name))
            return OperationResult.Fail("Student name is required.");

        var existing = await _studentRepository.GetByStudentNumberAsync(studentNumber, cancellationToken);
        if (existing is not null)
            return OperationResult.Fail($"Student number '{Student.NormalizeNumber(studentNumber)}' already exists.");

        try
        {
            var student = new Student(0, studentNumber, name, contactInfo, isAllowedToBorrow);
            await _studentRepository.AddAsync(student, cancellationToken);
            return OperationResult.Success();
        }
        catch (ArgumentException ex)
        {
            return OperationResult.Fail(ex.Message);
        }
    }

    // UC-13: Edit Student Details & Borrowing Eligibility
    public async Task<OperationResult> UpdateStudentAsync(
        int studentId,
        string studentNumber,
        string name,
        string? contactInfo,
        bool isAllowedToBorrow,
        CancellationToken cancellationToken = default)
    {
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
            return OperationResult.Fail("Selected student was not found.");

        if (string.IsNullOrWhiteSpace(studentNumber))
            return OperationResult.Fail("Student number is required.");
        if (string.IsNullOrWhiteSpace(name))
            return OperationResult.Fail("Student name is required.");

        var duplicate = await _studentRepository.GetByStudentNumberAsync(studentNumber, cancellationToken);
        if (duplicate is not null && duplicate.Id != studentId)
            return OperationResult.Fail($"Another student already uses student number '{Student.NormalizeNumber(studentNumber)}'.");

        try
        {
            student.UpdateDetails(studentNumber, name, contactInfo);
            student.SetEligibility(isAllowedToBorrow);
            await _studentRepository.UpdateAsync(student, cancellationToken);
            return OperationResult.Success();
        }
        catch (ArgumentException ex)
        {
            return OperationResult.Fail(ex.Message);
        }
    }

    // UC-14: View Student Borrowing History
    public async Task<IReadOnlyList<Borrowing>> GetStudentHistoryAsync(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        return await _borrowingRepository.GetByStudentIdAsync(studentId, cancellationToken);
    }
}