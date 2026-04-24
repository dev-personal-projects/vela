# Test Plan — Finance Tracker API (.NET + GraphQL)

## Purpose
This document defines a practical, end-to-end testing strategy for the Finance Tracker backend:

- Unit tests for business rules (fast, deterministic).
- Integration tests for persistence, auth, and GraphQL behavior (real dependencies).
- Clear “done” criteria for correctness, isolation, and error handling.

This plan is written so that a new contributor can implement tests incrementally without guessing what to cover.

## Goals (What “Works Perfectly” Means)
- Correctness: every feature behaves as specified on happy and failure paths.
- Isolation: users can never read or modify another user’s data.
- Reliability: mutations return consistent payloads (`errors`, optional `message`), and common failures (timeouts, auth, validation) are understandable.
- Performance guardrails: avoid known slow paths (full-row updates, extra round-trips); integration tests should catch regressions.
- Maintainability: tests are easy to read, quick to run locally, and stable in CI.

## Non-Goals
- Load testing at production scale (separate performance workstream).
- UI/client testing (out of scope for this backend repo).

## Testing Pyramid (Recommended Split)
- Unit tests (majority): `Application/Services/*` and small pure helpers.
- Integration tests (some): `Infrastructure/*` and DB behaviors.
- API/E2E (some): `Api/GraphQL/*` and auth enforcement using in-process HTTP.

## End-to-End (E2E) Tests
E2E tests validate that the entire system behaves correctly from an external client perspective:

- HTTP request -> auth middleware -> GraphQL pipeline -> services -> repositories -> real database
- Real serialization/deserialization and HTTP status codes
- Real cross-user isolation guarantees

### What Counts As E2E Here
Recommended (fast and reliable):
- In-process server using `WebApplicationFactory` with a real Postgres test database

Optional (slower, but closer to prod):
- Out-of-process server (`dotnet run`) + tests hit `http://localhost:8080` (smoke suite)

Not recommended for PR gating:
- Calling real Supabase Auth in E2E tests (flaky external dependency). Prefer test JWTs.

### E2E Setup Strategy
E2E tests should:
- Use a real Postgres database (container/service) with a clean schema per run.
- Use a fixed `Auth:JwtSecret` and generate JWTs in tests (no Supabase dependency).
- Seed minimal data per scenario and clean up between tests (transaction rollback or database reset).

Quickstart commands and env vars:
- `docs/E2E-QUICKSTART.md`

### E2E Scenarios (Must-Have)
These tests should use HTTP requests (GraphQL POST) and assert both response body and status code.

Auth and Access Control:
- Unauthenticated request to protected query/mutation returns 401.
- Expired token returns 401.
- Valid token returns 200.

Cross-User Isolation:
- User A creates income/expense/savings/loan.
- User B cannot read it (list excludes it; get-by-id returns null).
- User B cannot update/delete it (payload returns not found; data remains unchanged).

Mutation Contracts (User-Friendly Responses):
- Successful update returns payload with `message` and `errors == null`.
- Validation failure returns payload with `errors` and `message == null`.

CRUD Journeys (Happy Path):
- Income: add -> list -> update -> get-by-id -> delete -> verify missing.
- Expense: add -> list -> update -> delete -> verify missing.
- Savings: create -> update progress -> update fields -> delete -> verify missing.
- Loan: add -> record repayment -> update -> delete -> verify missing.

Dashboard:
- Seed known records and assert totals match expected sums.

Resilience/Timeout (Regression Guard):
- Simulate a slow DB call and verify the API returns a timeout error with guidance to refresh before retrying.

### E2E Project (Proposed)
Add `Tests/FinanceTracker.E2ETests/` if you want E2E explicitly separated from API tests.
If you prefer fewer projects, keep E2E inside `FinanceTracker.ApiTests` and tag E2E tests via trait/category.

## Tooling
- Test framework: `xUnit`
- Mocking: `Moq` (or `NSubstitute` if preferred, but pick one)
- Assertions: built-in xUnit assertions (optionally add FluentAssertions later)
- API integration: `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`)
- DB integration:
  - Preferred: local Postgres container for tests (Testcontainers or docker-compose)
  - Acceptable: dedicated Supabase “test” project (never production)

## Proposed Test Project Layout
Create a top-level `Tests/` directory (separate projects is fine, but keep it simple):

```
Tests/
  FinanceTracker.UnitTests/
  FinanceTracker.IntegrationTests/
  FinanceTracker.ApiTests/
```

Within each project, mirror the app feature areas:

```
Income/
Expense/
Savings/
Loan/
Auth/
Dashboard/
```

## Test Data Strategy
- Prefer builders/factories for inputs and entities (avoid copy/paste).
- Use fixed timestamps in tests (no `DateTime.UtcNow` comparisons without control).
- Use deterministic GUIDs where helpful for readability.
- Seed minimal data for each test; clean up after each test.

Recommended helpers (to add as tests grow):
- `TestUser` builder (two users A/B for isolation tests).
- `IncomeBuilder`, `ExpenseBuilder`, `LoanBuilder`, `SavingsGoalBuilder`.
- `GraphQlRequest` helper (sends `{ query, variables }`).

## Unit Test Plan (Application Layer)
Target: `Application/Services/*Service.cs`

### Common Assertions (All Services)
- Validation failures return `AppResult.Fail(...)` with the expected message.
- “Not found” returns `Fail("... not found.")`.
- No cross-user access is possible when `userId` differs.

### IncomeService
- Add:
  - Amount <= 0 -> fail
  - Source empty -> fail
  - Future date -> fail
  - Valid input -> ok, calls repo `AddAsync`
- Update:
  - Missing record -> fail
  - Same validations as Add
  - Valid update -> ok, calls repo `UpdateAsync`
- Delete:
  - Repo returns false -> fail “not found”
  - Repo returns true -> ok

### ExpenseService
- Mirror Income patterns; validate category.

### SavingsGoalService
- Create:
  - Name empty -> fail
  - Target <= 0 -> fail
  - Valid -> ok
- Update:
  - Target < current -> fail
  - Other validations -> fail/ok
- UpdateProgress:
  - newAmount < 0 -> fail
  - newAmount > target -> fail
  - Valid -> ok
- Delete:
  - Repo bool respected (true ok / false not found)

### LoanService
- Add:
  - Counterparty empty -> fail
  - Amount <= 0 -> fail
  - DueDate <= Date -> fail
- Update:
  - Counterparty empty -> fail
  - DueDate <= Date -> fail
- RecordRepayment:
  - Amount <= 0 -> fail
  - Amount > remaining -> fail
  - Valid repayment -> ok
- Delete:
  - Repo bool respected

### AuthService
Unit tests should *not* call real Supabase.
- SignUp:
  - Invalid email -> fail
  - Short password (< 8) -> fail
  - Valid -> calls `ISupabaseAuthClient.SignUpAsync`
- SignIn:
  - Invalid email -> fail
  - Missing password -> fail
  - Auth client failure -> fail
  - Valid -> returns API token via `IApiTokenService`

## Integration Test Plan (Infrastructure + DB)
Target: repositories and EF mappings.

## High-Value Test Cases (Checklist)
Use this as a punch-list when implementing tests.

### Cross-Cutting
- `Auth:JwtSecret` present and works for test tokens.
- Every `[Authorize]` GraphQL field rejects:
  - missing token
  - expired token
  - invalid signature
- Every mutation returns consistent payload shape:
  - success: `errors == null`, `message != null`
  - failure: `errors != null`, `message == null`

### Income/Expense
- Update does not change immutable fields (`created_at`, `user_id`).
- Delete is single-roundtrip and returns not-found for wrong user.
- Date cannot be in the future (unit tests).

### Savings
- Update target cannot go below current amount.
- Progress cannot exceed target.

### Loans
- Due date must be after loan date (create/update).
- Repayment cannot exceed remaining balance.
- Repayment updates `amount_repaid` and derived values behave correctly.

### Timeout/Resilience
- Simulated DB delay surfaces `REQUEST_TIMEOUT` (or payload `errors`) with guidance to refresh before retrying.

### DB Environment (Pick One)
Option A (Recommended for CI): Postgres container per test run
- Pros: hermetic, repeatable, cheap
- Cons: adds Docker dependency

Option B: dedicated Supabase test project
- Pros: matches production environment
- Cons: external dependency, flakier; must ensure cleanup

### Repository Tests (All Entities)
For each repository:
- `GetAllAsync` returns only the user’s rows.
- `GetByIdAsync` respects both `id` and `userId`.
- `AddAsync` persists with correct column mapping.
- `UpdateAsync` actually updates intended fields.
- `DeleteAsync`:
  - returns `false` when missing/wrong user
  - returns `true` when deleted
  - verifies row is removed

### EF Mapping Tests (High Value)
- `created_at` and `user_id` are not rewritten on update (regression guard).
- `numeric(18,2)` precision is preserved for amounts.

## API / GraphQL Integration Test Plan
Target: GraphQL pipeline correctness, auth enforcement, and payload contract.

## Coverage Matrix (What We Test Where)
This matrix helps prevent gaps and prevents duplicating work in the wrong layer.

| Area | Unit (Services) | Integration (Repos/DB) | API (GraphQL/HTTP) |
|---|---|---|---|
| Income | ✅ validations, not-found, delete result | ✅ CRUD + scoping + delete bool | ✅ auth + payload + isolation |
| Expense | ✅ validations, not-found, delete result | ✅ CRUD + scoping + delete bool | ✅ auth + payload + isolation |
| Savings | ✅ create/update/progress rules | ✅ CRUD + progress persistence | ✅ auth + payload + isolation |
| Loans | ✅ due date + repayment rules | ✅ CRUD + repayment persistence | ✅ auth + payload + isolation |
| Auth | ✅ input validation + token issuance | ⚠️ minimal (mock supabase) | ✅ reject missing/expired token |
| Dashboard | ✅ optional (pure aggregation helpers) | ✅ optional seed verification | ✅ expected totals from seeded data |
| Error handling | ✅ service error messages | ✅ DB errors mapped correctly | ✅ timeout/auth mapped consistently |

Legend:
- ✅ required for “done”
- ⚠️ keep thin to avoid flaky external calls

### Auth Enforcement
For every protected query/mutation:
- No JWT -> 401 (or GraphQL auth error mapped to 401)
- Expired JWT -> 401
- Valid JWT -> 200 and operation succeeds

### Payload Contract (Mutations)
For add/update/delete mutations:
- On success:
  - payload entity is not null (except deletes)
  - `errors` is null
  - `message` is a user-friendly success string
- On validation failure:
  - entity is null
  - `errors` contains the message
  - `message` is null

### Isolation (Critical)
For each entity type:
- User A creates data
- User B cannot read it (`GetAll` doesn’t include it; `GetById` returns null)
- User B cannot update/delete it (returns “not found” or equivalent)

### Error/Timeout Scenarios (Regression Guards)
- When DB operation times out or is canceled:
  - API surfaces `REQUEST_TIMEOUT` (or payload `errors` for update paths if handled there)
  - error contains guidance to refresh/check state before retry

## Dashboard Tests (API Level)
Given seeded incomes/expenses/savings/loans:
- `TotalIncome` = sum(incomes.amount)
- `TotalExpenses` = sum(expenses.amount)
- `NetBalance` = income - expenses
- `SavingsProgress` calculations stable
- `LoanSummary` totals correct (owed vs given)

## Manual Smoke Tests (Docs/Postman)
For quick human verification and demos, keep the Postman examples current:
- `docs/postman/test-examples.json`

These are not a substitute for automated tests, but they are useful for:
- sanity-checking deployments
- verifying auth/token flows
- reproducing issues quickly

## Test Execution (Local)
Proposed commands (once projects exist):
- `dotnet test Tests/FinanceTracker.UnitTests/FinanceTracker.UnitTests.csproj`
- `dotnet test Tests/FinanceTracker.IntegrationTests/FinanceTracker.IntegrationTests.csproj`
- `dotnet test Tests/FinanceTracker.ApiTests/FinanceTracker.ApiTests.csproj`
- `dotnet test Tests/FinanceTracker.E2ETests/FinanceTracker.E2ETests.csproj`

### Test Configuration (Recommended)
Prefer injecting config in tests rather than relying on developer machine settings:
- `Auth:JwtSecret`: fixed test value (>= 32 chars).
- `ConnectionStrings:Default`: points to local Postgres container.
- `Supabase:*`: can be dummy values for most tests (auth client is mocked in unit tests).

## CI Recommendations
- Run unit tests on every PR.
- Run integration + API tests on PRs that touch `Infrastructure/`, `Api/`, or migrations/scripts.
- Run E2E tests on PRs for backend changes (or nightly if runtime is too slow).
- Cache NuGet packages.
- Use a Postgres service container for integration tests (preferred).

### CI Outline (Example)
High-level workflow (details depend on how we set up the `Tests/*` projects):
- Restore/build
- Unit tests
- Start Postgres service
- Apply schema/migrations/seed
- Integration tests + API tests

Example GitHub Actions shape (illustrative):

```yaml
services:
  postgres:
    image: postgres:16
    env:
      POSTGRES_PASSWORD: postgres
    ports: ["5432:5432"]
```

### Stability Rules (Avoid Flaky Tests)
- Unit tests must not use real network calls.
- Integration tests must not rely on shared external environments unless explicitly marked and excluded from PR runs.
- Prefer polling with a short timeout for “eventual consistency” over fixed sleeps.
- Keep timeouts explicit and generous enough for CI, but short enough to fail fast on deadlocks.
- Keep E2E suite lean: 1-2 journeys per feature + isolation + dashboard is usually enough.

## Exit Criteria
- All tests pass locally and in CI.
- No cross-user data leakage in any feature.
- Mutations return consistent `errors`/`message` behavior.
- A contributor can run tests with a single command and get deterministic results.
