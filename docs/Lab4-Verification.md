# Lab 4 - Verification, Traceability and Evidence

Use-case diagram: `docs/use-case-diagram.png` (source `docs/use-case-diagram.mmd`).

## 1. Setup for every test run
1. Copy `EquipmentBorrowing.Desktop/appsettings.example.json` to `appsettings.json` and put the real PostgreSQL password in it (the file is git-ignored).
2. `dotnet run --project .\EquipmentBorrowing.Desktop\EquipmentBorrowing.Desktop.csproj` - on first start the app applies the migrations and inserts demo data only if the tables are empty.
3. Unexpected errors are written to `%LOCALAPPDATA%\EquipmentBorrowing\error.log`.
4. Read-back check: run the queries in `docs/database-queries.txt.txt` in pgAdmin after each test.

## 2. Traceability (use case -> implementation)
| UC | Where it lives | Status |
|----|----------------|--------|
| UC-01 View catalog | `EquipmentViewModel.LoadAsync`, `EquipmentView` (loading / error / empty states) | Implemented |
| UC-02 Filter available | `EquipmentViewModel.ShowOnlyAvailable` (re-reads DB, then filters) | Implemented |
| UC-03 Borrow | `BorrowEquipmentService` (one transaction: borrowing row + availability) | Implemented, fixed |
| UC-04 Active borrowings | `BorrowingsViewModel` | Implemented |
| UC-05 Return | `ReturnEquipmentService` (one transaction) | Implemented |
| UC-06 Multi-item checkout | `CheckoutMultipleEquipmentService` (all or nothing, Serializable transaction) | Implemented |
| UC-07 Student history | `StudentManagementViewModel.LoadHistoryForStudentAsync` | Implemented |
| UC-08 Equipment history | `BorrowingQueryService.GetEquipmentHistoryAsync`, Manage Equipment screen | Implemented |
| UC-09 Search/filter/sort | `BorrowingSearchViewModel`, `BorrowingQueryRepository` (SQL-side) | Implemented |
| UC-12..UC-16 | `StudentManagementService`, `EquipmentManagementService` | Implemented (partner) |
| UC-10/11/others | See the lab specification | Check against the PDF |

## 3. Rule -> test map (fill the "Observed" column yourself while running the app)
| Test | Steps | Expected | Observed | Screenshot |
|------|-------|----------|----------|-----------|
| A01 empty vs error | Stop PostgreSQL, open Equipment | Red error banner with Retry, NOT "no equipment" | | |
| A02 filter | Tick "Show available only" after borrowing one item | Borrowed item hidden; untick shows it | | |
| A03 borrow | Claire + Laptop, date +7 | Success; Laptop shows Borrowed; DB: 1 Active row, `IsAvailable=false` | | |
| A03b date | Return date = today | "must be later than today", nothing saved | | |
| A03c restricted | Student Jack | Rejected: not permitted | | |
| A03d limit | 4th active item for one student | Rejected: limit of 3 | | |
| A05 return | Return the Laptop | Status Returned; Laptop Available again | | |
| A05b double return | Return stale row | "already returned", no change | | |
| A06 multi checkout | Tick 2 available items | Both Borrowed, 2 Active rows | | |
| A06b reject | Tick 1 available + 1 borrowed item | Rejected naming the item; DB unchanged | | |
| A06c rollback | Set env var `EBS_FAIL_BEFORE_COMMIT=1`, restart, check out 2 items | "could not be saved, nothing changed"; DB has no new rows | | |
| A07 / A08 history | Select student / equipment | Rows match DB; empty state when none | | |
| A09 search | "claire", status Active, sort Expected return asc | Only matching rows, stable order; Reset restores all | | |
| A09b no match | search "zzzz" | "No borrowings match your search." (not an error) | | |
| Contrast | Windows dark mode on | All text readable, no black lists | | |
Remove the `EBS_FAIL_BEFORE_COMMIT` variable after A06c.

## 4. Known limits
- Not compiled or run by the author of this file; observed results must come from your own runs.
- Concurrent-user conflicts rely on PostgreSQL Serializable transactions and the unique active-item index; they are reported as "Conflict - nothing saved".
