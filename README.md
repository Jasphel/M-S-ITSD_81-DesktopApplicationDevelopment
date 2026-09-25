# Equipment Borrowing System

Laboratory Activity 1 — ITSD 81, Desktop Application Development.

This repository contains the initial architectural foundation for a campus
equipment borrowing system. No database and no graphical interface are
implemented yet — the goal of this activity was to get the responsibilities
of the system separated correctly *before* either of those are added.

---

## Objectives → Evidence

| # | Objective | Evidence |
|---|---|---|
| 1 | Create and configure a structured .NET solution | `EquimentBorrowingSys.slnx` lists all four projects; each `.csproj`'s `<ProjectReference>` entries define which project can see which (Domain has none, Application references only Domain, Infrastructure references Application+Domain, the exe references Application+Infrastructure) |
| 2 | Identify domain concepts from a simple system scenario | `EquipmentBorrowing.Domain/Student.cs`, `Equipment.cs`, `Borrowing.cs`, `BorrowingStatus.cs` — one type per concept from the scenario |
| 3 | Translate responsibilities into domain models, application services, repository abstractions | Domain models as above; `EquipmentBorrowing.Application/Services/BorrowEquipmentService.cs` (the use case); `EquipmentBorrowing.Application/Interfaces/IStudentRepository.cs`, `IEquipmentRepository.cs`, `IBorrowingRepository.cs` (the abstractions) |
| 4 | Apply classes, records, interfaces, nullable reference types, async signatures | Classes and interfaces throughout; `<Nullable>enable</Nullable>` in every `.csproj`, used in `IStudentRepository.cs`'s `Task<Student?> GetByIdAsync(...)`; async signatures on every repository method, awaited in `BorrowEquipmentService.BorrowEquipmentAsync`. **Not yet used: `record`** — worth adding or explicitly noting as a design choice |
| 5 | Organize code by separation of concerns | `BorrowEquipmentService`'s constructor depends only on the three `I...Repository` interfaces, never on the concrete `InMemory...Repository` classes in `EquipmentBorrowing.Infrastructure/Repositories/` — business rules and storage are in different projects entirely |
| 6 | Explain how this supports later desktop UI and database work | `EquimentBorrowingSys/Program.cs` constructs repositories and calls `BorrowEquipmentService.BorrowEquipmentAsync` — a future Avalonia button handler or a future `SqliteStudentRepository` would slot into this exact same pattern without changing `BorrowEquipmentService.cs` itself |

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

---

# Laboratory Activity 2: Extending with Avalonia UI and MVVM

This section extends the explanation above — it does not replace it. Everything
in Laboratory Activity 1 (Domain, Application, Infrastructure) is unchanged in
its responsibilities; a presentation layer has been added on top of it.

## 1. Desktop Project

`EquipmentBorrowing.Desktop` is the only project in the solution that is
allowed to know Avalonia exists. Its job is limited to:

- displaying information (Views);
- collecting user input (Views + bindings);
- holding presentation state and issuing commands (ViewModels); and
- wiring everything together at startup (the composition root in
  `App.axaml.cs`).

It references `EquipmentBorrowing.Application` (for the services and
repository interfaces) and `EquipmentBorrowing.Infrastructure` (only inside
the composition root, to register the concrete in-memory repositories).
`EquipmentBorrowing.Domain` and `EquipmentBorrowing.Application` do not
reference `EquipmentBorrowing.Desktop`, and do not reference Avalonia at all
— the dependency only points one way, toward the UI.

**Two implementation notes worth being upfront about, since a reviewer would
ask about them anyway:**

- `BorrowEquipmentService.BorrowEquipmentAsync` used to return a plain
  `bool`. That can't tell a caller *why* something failed, and Part I of
  this activity requires distinguishing failure reasons (equipment
  unavailable vs. student not permitted vs. limit reached) without
  reimplementing those checks in the ViewModel. It now returns a
  `BorrowResult` (in `EquipmentBorrowing.Application/Results/`) carrying a
  `BorrowFailureReason`. The same pattern (`ReturnResult` /
  `ReturnFailureReason`) is used for the new return use case. This is a
  refactor of existing Lab 1 code, done for a specific, documented reason,
  not a rewrite.
- Nothing in Lab 1 generated a `Borrowing.Id` — the console demo just typed
  `1` and `2` by hand. A real UI can't do that, so `IBorrowingRepository`
  gained a `GetNextIdAsync()` method. This lives in the repository (not the
  ViewModel) because ID generation is a storage concern — a real database
  would do this itself via an auto-increment column.

## 2. Updated Architecture

```text
Avalonia View (Views/*.axaml)
          │
          │  Binding / Command
          ▼
     ViewModel (ViewModels/*.cs)
          │
          │  await _service.SomethingAsync(...)
          ▼
   Application Service (BorrowEquipmentService / ReturnEquipmentService)
          │
          ├──────────► Domain (Student, Equipment, Borrowing)
          │
          ▼
   Repository Interface (I...Repository, in Application/Interfaces)
          ▲
          │
   Infrastructure Implementation (InMemory...Repository)
```

Dependency direction, spelled out:

```text
EquipmentBorrowing.Desktop
          │
          ├──────────► EquipmentBorrowing.Application
          │
          └──────────► EquipmentBorrowing.Infrastructure
                              │
                              ▼
                    (implements) Repository Interfaces
                              │
                              ▼
                    EquipmentBorrowing.Application
                              │
                              ▼
                    EquipmentBorrowing.Domain
```

`EquipmentBorrowing.Domain` still depends on nothing. `Application` still
depends only on `Domain`. The only thing that changed is a new project sits
*above* everything else, depending inward — nothing below it knows it exists.

## 3. Borrow Equipment Flow

1. The user opens the **Equipment** screen. `MainWindowViewModel` has
   already called `EquipmentViewModel.LoadAsync()`, which asked
   `IEquipmentRepository` and `IStudentRepository` for everything currently
   stored, and filled two `ObservableCollection`s the View is bound to.
2. The user clicks a row in the equipment `ListBox` — this sets
   `SelectedEquipment` through a two-way binding. They pick a student from
   the `ComboBox` (`SelectedStudent`) and a date from the `DatePicker`
   (`ExpectedReturnDate`).
3. The user clicks **Borrow Equipment**, which is bound to
   `EquipmentViewModel.BorrowCommand` (generated by `[RelayCommand]` from
   the `BorrowAsync` method).
4. `BorrowAsync` first checks that a student, equipment, and a valid date
   were actually selected. This is *presentation* validation — it never
   asks whether borrowing is *allowed*, only whether the form is complete
   enough to ask.
5. It asks `IBorrowingRepository` for the next id, then calls
   `_borrowEquipmentService.BorrowEquipmentAsync(...)` — the exact same
   method Lab 1's console demo calls.
6. `BorrowEquipmentService` runs the real rules (student exists, student
   allowed to borrow, under the borrowing limit, equipment exists,
   equipment available) entirely inside the Application layer, and returns
   a `BorrowResult`.
7. Back in the ViewModel, `StatusMessage` is set from that result —
   `DescribeFailure(...)` just turns an enum value into a sentence, it does
   not decide anything. On success, the equipment list reloads (its
   `IsAvailable` has changed) and the View updates automatically because
   it's bound to an `ObservableCollection`.

## 4. Return Equipment Flow

1. The user switches to the **Active Borrowings** screen.
   `MainWindowViewModel` calls `BorrowingsViewModel.LoadAsync()`, which
   fetches active borrowings and joins them (in the ViewModel, for display
   only) against student and equipment names into `BorrowingListItem`
   rows.
2. The user selects a row (`SelectedBorrowing`) and clicks **Return
   Equipment**, bound to `BorrowingsViewModel.ReturnCommand`.
3. `ReturnAsync` checks a row is actually selected (presentation
   validation), then calls the new
   `_returnEquipmentService.ReturnEquipmentAsync(SelectedBorrowing.Id)`.
4. `ReturnEquipmentService` looks up the borrowing, checks it isn't already
   returned, looks up its equipment, then calls `borrowing.MarkAsReturned()`
   and `equipment.MarkAsAvailable()` — both domain methods that already
   existed from Lab 1, just never wired to anything until now.
5. The ViewModel sets `StatusMessage` from the `ReturnResult`, clears the
   selection, and reloads the borrowings list. The Equipment screen picks up
   the newly-available equipment the next time the user navigates to it,
   since `MainWindowViewModel` reloads each screen's data on every
   navigation.

## 5. Architectural Reflection

**1. Why should the View not call a repository directly?**

The View only knows how to draw controls and raise bindings/commands — it
has no way to judge whether an operation is valid. If a `Button.Click`
handler called `IEquipmentRepository` directly, the borrowing rules would
either have to be duplicated inside the code-behind or skipped entirely.
Going through a ViewModel that calls an Application Service keeps exactly
one place responsible for deciding whether a borrow or return is allowed.

**2. Why should business rules not be implemented in the ViewModel?**

The ViewModel's job is presentation state — what's selected, what message
to show, which command is currently running. The moment it starts checking
`equipment.IsAvailable && student.IsAllowedToBorrow` itself, that logic now
exists in two places (the ViewModel *and* `BorrowEquipmentService`), which
will eventually disagree with each other as one gets updated and the other
doesn't. Worse, any other future front end (a web UI, a mobile app) would
have to reimplement the same rules again from scratch instead of reusing
the service.

**3. What is the responsibility of the ViewModel?**

To sit between the View and the Application layer: hold the data the View
binds to (`ObservableCollection`s, selected values), expose commands the
View can invoke, run *presentation* validation (is a field empty, is a
selection missing), and translate the result of an application operation
into something displayable. It never decides *whether* an operation is
allowed — only whether it's *worth asking*.

**4. Why can the existing Application layer work without knowing that
Avalonia is being used?**

Because `BorrowEquipmentService` and `ReturnEquipmentService` only depend on
`IStudentRepository`, `IEquipmentRepository`, and `IBorrowingRepository` —
interfaces that live in `EquipmentBorrowing.Application` itself, with zero
reference to Avalonia anywhere in that project. The Desktop project depends
on Application; Application has no idea Desktop exists. The same services
that ran inside a console app in Lab 1 run unmodified inside a GUI in Lab 2.

**5. What advantage is gained from registering dependencies in one
composition point?**

`App.axaml.cs`'s `ConfigureServices` is the only place in the entire
solution that ever writes `new InMemoryStudentRepository()` (or its
`Sqlite...` equivalent, later). Every ViewModel and service just declares
"I need an `IStudentRepository`" in its constructor and lets the container
supply it. That means swapping an implementation, or changing whether a
repository is shared (`Singleton`) or fresh-per-use (`Transient`), is a
one-line change in one file, instead of a search-and-replace across the
whole codebase.

**6. If the in-memory repository were replaced by SQLite later, which parts
of the current interface should remain largely unchanged?**

All of it, except the composition root. `EquipmentView.axaml`,
`BorrowingsView.axaml`, `EquipmentViewModel`, `BorrowingsViewModel`,
`BorrowEquipmentService`, and `ReturnEquipmentService` would not need to
change at all — they only ever talk to the repository *interfaces*. Only
`ConfigureServices` in `App.axaml.cs` would change, to register
`SqliteStudentRepository : IStudentRepository` instead of
`InMemoryStudentRepository`.
