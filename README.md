# M-S-ITSD_81-DesktopApplicationDevelopment
This repository is for our ITSD81. I hope whoever views this to have a good day!
NOTE: This project was made first in the visual studio, face-to-face by the pair-collectively. After that, it was pushed in Github by one account.
Members: Mancawan, Claire Jasper D.
         Solon, Shekinah Myrrh O.


# Equipment Borrowing System

Laboratory Activity 1 — ITSD 81, Desktop Application Development.

This repository contains the initial architectural foundation for a campus
equipment borrowing system. No database and no graphical interface are
implemented yet — the goal of this activity was to get the responsibilities
of the system separated correctly *before* either of those are added.

---

## 1. Solution Structure

The solution is split into four projects, each with one job:

- **`EquipmentBorrowing.Domain`**
  Holds the core concepts of the problem itself: `Student`, `Equipment`,
  `Borrowing`, and `BorrowingStatus`. These classes know nothing about how
  they are stored, displayed, or requested — they only protect their own
  rules. For example, `Equipment.IsAvailable` can only change through
  `MarkAsBorrowed()` / `MarkAsAvailable()`, never by direct assignment.

- **`EquipmentBorrowing.Application`**
  Holds the actual use case: `BorrowEquipmentService`. This is where the
  borrowing rules from the scenario are enforced (student exists, student is
  allowed to borrow, student hasn't hit the borrowing limit, equipment
  exists, equipment is available). It also defines the repository
  *interfaces* (`IStudentRepository`, `IEquipmentRepository`,
  `IBorrowingRepository`) — the contracts the application needs, without
  saying how they're fulfilled.

- **`EquipmentBorrowing.Infrastructure`**
  Holds the current implementations of those interfaces:
  `InMemoryStudentRepository`, `InMemoryEquipmentRepository`,
  `InMemoryBorrowingRepository`. Each one just wraps a `List<T>`. This is
  the only layer that would need to change if a real database were added
  later.

- **`EquimentBorrowingSys`** (executable)
  The entry point. `Program.cs` wires the repositories and the service
  together and runs one successful borrow and one failed borrow to
  demonstrate the flow end to end.

There is not yet a dedicated `EquipmentBorrowing.Tests` project — this is
still outstanding for this activity.

---

## 2. Dependency Direction

```text
EquimentBorrowingSys (Program.cs)
          │
          ▼
EquipmentBorrowing.Infrastructure
          │
          ▼
EquipmentBorrowing.Application
          │
          ▼
EquipmentBorrowing.Domain
```

`Domain` depends on nothing else in the solution. `Application` depends only
on `Domain`. `Infrastructure` depends on `Application` and `Domain` (it
needs the interfaces to implement, and the domain objects to store). The
executable depends on all three, but only to construct objects and start the
program — it doesn't contain any business rules itself.

This direction is what makes the repository swap in Reflection Q2 possible:
nothing below `Infrastructure` in this diagram knows `Infrastructure`
exists.

---

## 3. Use Case Mapping

```text
Actor:                            Student
Use Case:                         Borrow Equipment
Application Service:              BorrowEquipmentService.BorrowEquipmentAsync
Domain Objects Used:              Student, Equipment, Borrowing, BorrowingStatus
Repository Interfaces Used:       IStudentRepository, IEquipmentRepository, IBorrowingRepository
Infrastructure Implementations:   InMemoryStudentRepository, InMemoryEquipmentRepository, InMemoryBorrowingRepository
```

`Program.cs` demonstrates this use case twice: once where the borrow
succeeds (equipment id `1` is available), and once immediately after where
the same borrow fails, because the equipment is no longer available.

---

## 4. Reflection

**1. Why should the application service depend on a repository interface
instead of directly depending on a database implementation?**

`BorrowEquipmentService` only calls methods that exist on
`IStudentRepository`, `IEquipmentRepository`, and `IBorrowingRepository`. It
never references a `List<T>`, a connection string, or any SQL. Because of
that, the service has no idea whether the data is coming from an in-memory
list, SQLite, or something else entirely — and it doesn't need to. This
keeps the business rules isolated from storage details, and makes the
service possible to test without needing a real database running.

**2. Which parts of your current solution could remain unchanged if SQLite
were added later?**

Everything in `EquipmentBorrowing.Domain` and `EquipmentBorrowing.Application`
would stay exactly as it is, including `BorrowEquipmentService` itself.
Adding SQLite would only mean adding new classes in
`EquipmentBorrowing.Infrastructure` (e.g. `SqliteStudentRepository :
IStudentRepository`) and changing which repository gets constructed in
`Program.cs`.

**3. Which project would eventually contain Avalonia Views?**

A new UI project (or `EquimentBorrowingSys` itself, if it were converted
into an Avalonia app) sitting above `EquipmentBorrowing.Application` in the
dependency diagram. It would call `BorrowEquipmentService` the same way
`Program.cs` currently does — the UI layer triggers the use case, it doesn't
implement it.

**4. Should an Avalonia button directly execute database queries? Why or why
not?**

No. Doing that would collapse the separation this whole activity is built
around: the UI would need to know SQL and storage details, and the
borrowing rules (student allowed to borrow, equipment available, max active
borrowings) would have nowhere consistent to live — they'd either be
duplicated in the UI code or skipped entirely. A button should only ever
call an application service, the same way `Program.cs` does.

**5. What part of your implementation represents the actual business
operation requested by the actor?**

`BorrowEquipmentService.BorrowEquipmentAsync`. Everything above it
(`Program.cs`, and eventually a UI button) is just *triggering* the
operation. Everything below it (the repositories) is just *fetching and
storing* data on its behalf. The method itself is where the actual decision
— can this student borrow this equipment right now — gets made.
