using System;
using EquipmentBorrowing.Application.Services;
using EquipmentBorrowing.Domain;
using EquipmentBorrowing.Infrastructure.Repositories;

var studentRepository = new InMemoryStudentRepository();
var equipmentRepository = new InMemoryEquipmentRepository();
var borrowingRepository = new InMemoryBorrowingRepository();

// Seed Students (id, studentNumber, name, contactInfo, isAllowedToBorrow)
studentRepository.Add(new Student(1, "STU-001", "Shekinah", null, true));
studentRepository.Add(new Student(2, "STU-002", "Leila", null, true));
studentRepository.Add(new Student(3, "STU-003", "Fluffy", null, true));
studentRepository.Add(new Student(4, "STU-004", "Rajah", null, false)); // not allowed to borrow, for testing

// Seed Equipment (id, identifier, name, description, isAvailable)
equipmentRepository.Add(new Equipment(1, "EQP-001", "Laptop"));
equipmentRepository.Add(new Equipment(2, "EQP-002", "Projector"));
equipmentRepository.Add(new Equipment(3, "EQP-003", "HDMI Cable"));
equipmentRepository.Add(new Equipment(4, "EQP-004", "Keyboard"));
equipmentRepository.Add(new Equipment(5, "EQP-005", "Tripod"));

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