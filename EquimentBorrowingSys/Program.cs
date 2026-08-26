using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Infrastructure.Repositories;
using EquipmentBorrowing.Domain;

var studentRepository = new InMemoryStudentRepository();
var equipmentRepository = new InMemoryEquipmentRepository();
var borrowingRepository = new InMemoryBorrowingRepository();

var student = new Student(
    1,
    "Shekinah",
    true);

studentRepository.Add(student);

studentRepository.Add(new Student(2, "Leila", true));
studentRepository.Add(new Student(3, "Fluffy", true));
studentRepository.Add(new Student(4, "Rajah", false)); // not allowed to borrow, for testing that rule later

var equipment = new Equipment(
    1,
    "Laptop");

equipmentRepository.Add(equipment);

equipmentRepository.Add(new Equipment(2, "Projector"));
equipmentRepository.Add(new Equipment(3, "HDMI Cable"));
equipmentRepository.Add(new Equipment(4, "Keyboard"));
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
    successfulBorrow
        ? "SUCCESS: Equipment was borrowed."
        : "FAILED: Equipment could not be borrowed.");

var failedBorrow = await borrowEquipmentService.BorrowEquipmentAsync(
    borrowingId: 2,
    studentId: 1,
    equipmentId: 1,
    expectedReturnDate: DateTime.Now.AddDays(7));

Console.WriteLine(
    failedBorrow
        ? "SUCCESS: Equipment was borrowed."
        : "FAILED: Equipment is unavailable.");
