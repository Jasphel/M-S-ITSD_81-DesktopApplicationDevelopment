using System;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

var studentRepository = new InMemoryStudentRepository();
var equipmentRepository = new InMemoryEquipmentRepository();
var borrowingRepository = new InMemoryBorrowingRepository();

// Seed Students
studentRepository.Add(new Student(1, "Shekinah", true));
studentRepository.Add(new Student(2, "Leila", true));
studentRepository.Add(new Student(3, "Fluffy", true));
studentRepository.Add(new Student(4, "Rajah", false)); // not allowed to borrow, for testing

// Seed Equipment
equipmentRepository.Add(new Equipment(1, "Laptop"));
equipmentRepository.Add(new Equipment(2, "Projector"));
equipmentRepository.Add(new Equipment(3, "HDMI Cable"));
equipmentRepository.Add(new Equipment(4, "Keyboard"));
equipmentRepository.Add(new Equipment(5, "Tripod"));

var borrowEquipmentService = new BorrowEquipmentService(
    studentRepository,
    equipmentRepository,
    borrowingRepository);

// First Borrow Attempt (Valid)
var successfulBorrow = await borrowEquipmentService.BorrowEquipmentAsync(
    borrowingId: 1,
    studentId: 1,
    equipmentId: 1,
    expectedReturnDate: DateTime.Now.AddDays(7));

Console.WriteLine(
    successfulBorrow.Succeeded
        ? "SUCCESS: Equipment was borrowed."
        : $"FAILED: {successfulBorrow.FailureReason}");

// Second Borrow Attempt (Duplicate / Unavailable Equipment)
var failedBorrow = await borrowEquipmentService.BorrowEquipmentAsync(
    borrowingId: 2,
    studentId: 1,
    equipmentId: 1,
    expectedReturnDate: DateTime.Now.AddDays(7));

Console.WriteLine(
    failedBorrow.Succeeded
        ? "SUCCESS: Equipment was borrowed."
        : $"FAILED: {failedBorrow.FailureReason}");