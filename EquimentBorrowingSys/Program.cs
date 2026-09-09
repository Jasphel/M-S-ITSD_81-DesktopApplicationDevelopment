using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Repositories;
using EquipmentBorrowing.Domain;

var studentRepository = new InMemoryStudentRepository();
var equipmentRepository = new InMemoryEquipmentRepository();
var borrowingRepository = new InMemoryBorrowingRepository();

var student = new Student(
    1,
    "Claire",
    true);

studentRepository.Add(student);

studentRepository.Add(new Student(2, "Jasper", true));
studentRepository.Add(new Student(3, "Mancawan", true));
studentRepository.Add(new Student(4, "Jack", false)); // not allowed to borrow, for testing that rule later

var equipment = new Equipment(
    1,
    "Laptop");

equipmentRepository.Add(equipment);

equipmentRepository.Add(new Equipment(2, "Projector"));
equipmentRepository.Add(new Equipment(3, "HDMI Cable"));
equipmentRepository.Add(new Equipment(4, "DSLR Camera"));
equipmentRepository.Add(new Equipment(5, "Tripod"));

var borrowEquipmentService = new BorrowEquipmentService(
    studentRepository,
    equipmentRepository,
    borrowingRepository);

var successfulBorrow = await borrowEquipmentService.BorrowEquipmentAsync(
    borrowingId: 1,
    studentId: 1,
    equipmentId: 1,
    expectedReturnDate: DateTime.Now.AddDays(7));

Console.WriteLine(
    successfulBorrow.Succeeded
        ? "SUCCESS: Equipment was borrowed."
        : $"FAILED: {successfulBorrow.FailureReason}");

var failedBorrow = await borrowEquipmentService.BorrowEquipmentAsync(
    borrowingId: 2,
    studentId: 1,
    equipmentId: 1,
    expectedReturnDate: DateTime.Now.AddDays(7));

Console.WriteLine(
    failedBorrow.Succeeded
        ? "SUCCESS: Equipment was borrowed."
        : $"FAILED: {failedBorrow.FailureReason}");
