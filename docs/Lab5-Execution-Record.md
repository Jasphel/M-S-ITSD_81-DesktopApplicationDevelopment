# Laboratory Activity 5 - Execution Record

## Baseline and revision

- Lab 4 source baseline: commit `a5c636d` (`docs: Lab 4 verification, traceability matrix, use-case diagram, README`), present in the supplied public repository history.
- Lab 5 working revision: built on top of repository commit `b46a250` (`EquipmentBorrowing.Api` + `EquipmentBorrowing.Desktop` HTTP client migration). Create the `lab5-submitted` tag after final commit and push.
- Before/after: Lab 4 desktop ViewModels resolved application services and PostgreSQL repositories directly. `EquipmentBorrowing.Desktop` is now refactored to use `BorrowingApiClient` over HTTP (`http://localhost:5000`) across `MainWindowViewModel`, `EquipmentViewModel`, `EquipmentManagementViewModel`, `StudentManagementViewModel`, `BorrowingsViewModel`, and `BorrowingSearchViewModel`.
- Existing PostgreSQL entities, EF configurations, migrations, application rules, and `BorrowingTransactionFactory` are retained.

## Defects found, fixes applied, and retests

- **Duplicate code & constructor wiring during ViewModel HTTP migration:** Resolved duplicate members in `StudentManagementViewModel.cs`, missing closing/helper in `EquipmentManagementViewModel.cs`, and `MainWindowViewModel.StartAsync` signature mismatch in `App.axaml.cs`. Retested solution build and confirmed `0` build errors.
- **Npgsql `timestamptz` (`DateTimeKind`) rejection on write operations:** `BorrowEquipmentService.cs`, `CheckoutMultipleEquipmentService.cs`, and `ReturnEquipmentService.cs` passed `Local` or `Unspecified` `DateTime` values into PostgreSQL `timestamp with time zone` columns, causing `DbUpdateException` / `PersistenceFailed`. Normalized write timestamps and return dates to `DateTimeKind.Utc` (`DateTime.UtcNow` / UTC conversion) while keeping local calendar validation (`expectedReturnDate.Date <= DateTime.Today`). Retested single borrow, multi-item checkout, and item return in Avalonia against PostgreSQL; all succeeded and refreshed UI state.
- **Unreachable API vs. server error handling (T08):** Stopped `EquipmentBorrowing.Api` while `EquipmentBorrowing.Desktop` was open and clicked Refresh/Retry; confirmed `"Cannot reach the API server..."` appeared cleanly without crashing, then restarted the API and confirmed Retry reloaded live PostgreSQL data.

## Acceptance test results

| Test | Status | Notes / Evidence |
|---|---|---|
| T01 | Verified (API host) | `EquipmentBorrowing.Api` runs independently on `http://localhost:5000` backed by PostgreSQL |
| T02 | Verified (UI/API) | Catalog and `Show available only` filter load over HTTP and update availability on borrow/return |
| T03 | Verified (UI/API) | Single-item borrow (`EQ-1006` / `EQ-1007`) persists to PostgreSQL and updates item status |
| T04 | Verified (UI/API) | Active Borrowings list loads and item return updates status and refreshes state |
| T05 | Verified (UI/API) | Multi-item checkout (2 items) commits atomically via `CheckoutMultipleEquipmentService` and updates equipment status |
| T06 | Verified (UI/API) | Students, Manage Equipment (create/edit), and Search Borrowings tabs load and persist over HTTP |
| T07 | Verified (UI/API) | Desktop client operates over HTTP against `http://localhost:5000` with refresh after actions |
| T08 | Verified (UI/API) | Unreachable API displays connection error cleanly; restarting API and clicking Retry restores data |

## API traceability

The resource routes are mapped in `EquipmentBorrowing.Api/ApiRoutes.cs`. Application services are reused for checkout, return, student management, equipment management, and borrowing queries; `BorrowingTransactionFactory` remains the atomic checkout boundary, and `/openapi/v1.json` publishes the Minimal API contract.

| Lab 4 use case | Endpoint | Implementation reference | Status |
|---|---|---|---|
| UC-01, UC-02 | GET equipment | `ApiRoutes.MapEquipmentApi` | Verified via `EquipmentViewModel` |
| UC-03 | POST borrowings, GET borrowing | `ApiRoutes.MapBorrowingApi` | Verified via single borrow |
| UC-04, UC-09 | GET borrowings | `ApiRoutes.MapBorrowingApi` | Verified via Active Borrowings & Search |
| UC-05 | POST borrowing return | `ApiRoutes.MapBorrowingApi` | Verified via return action |
| UC-06 | POST checkout-requests | `ApiRoutes.MapBorrowingApi` | Verified via multi-item checkout |
| UC-07 | student list/detail/history | `ApiRoutes.MapStudentApi` | Verified via Students tab |
| UC-08 | equipment list/detail/history | `ApiRoutes.MapEquipmentApi` | Verified via Manage Equipment tab |
| UC-12-UC-14 | student POST/PUT/eligibility | `ApiRoutes.MapStudentApi` | Verified via create/update student |
| UC-15-UC-16 | equipment POST/PUT | `ApiRoutes.MapEquipmentApi` | Verified via add/edit equipment |

## Build check

Solution builds cleanly with `0` errors after HTTP ViewModel migration and UTC `DateTime` fixes.