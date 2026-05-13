# Test Plan — Finance Tracker API (.NET + GraphQL)

## Purpose
This document defines a practical, end-to-end testing strategy for the Finance Tracker backend:

- Unit tests for business rules (fast, deterministic).
- Integration tests for persistence, auth, and GraphQL behavior (real dependencies).
- Clear “done” criteria for correctness, isolation, and error handling.

This plan is written so that a new contributor can implement tests incrementally without guessing what to cover.

Related focused plans:
- [test-requirements.md](/workspaces/finance-tracker-graphql/docs/test-requirements.md)
- [unit-test-plan.md](/workspaces/finance-tracker-graphql/docs/unit-test-plan.md)
- [integration-test-plan.md](/workspaces/finance-tracker-graphql/docs/integration-test-plan.md)
- [testing-conventions.md](/workspaces/finance-tracker-graphql/docs/testing-conventions.md)
- [ci-plan.md](/workspaces/finance-tracker-graphql/docs/ci-plan.md)

## Testing Standard

This codebase standardizes on **xUnit.net v3** for automated tests.

That is the default and recommended framework for:
- unit tests
- integration tests
- future API/E2E test layers

Why xUnit:
- modern .NET ecosystem alignment
- strong IDE and `dotnet test` support
- simple attribute model
- clean parallelization and trait support
- good fit for long-term maintainability in a professional .NET codebase

## Goals (What “Works Perfectly” Means)
- Correctness: every feature behaves as specified on happy and failure paths.
- Isolation: users can never read or modify another user’s data.
- Reliability: mutations return consistent payloads (`errors`, optional `message`), and common failures (timeouts, auth, validation) are understandable.
- Performance guardrails: avoid known slow paths (full-row updates, extra round-trips); integration tests should catch regressions.
- Maintainability: tests are easy to read, quick to run locally, and stable in CI.

## Non-Goals
- Load testing at production scale (separate performance workstream).
- UI/client testing (out of scope for this backend repo).

## Current Scope
For the next testing phase, the focus is:

- unit tests
- integration tests
- API-level GraphQL integration tests

E2E coverage is intentionally deferred to a later iteration. API-level GraphQL tests are not full E2E tests; they are backend pipeline tests and should be added before browser/client E2E work.

## Testing Pyramid (Current Split)
- Unit tests (majority): `Application/Services/*` and small pure helpers.
- Integration tests (some): `Infrastructure/*`, database behavior, and cache integration behavior.
- API integration tests (targeted): GraphQL/HTTP auth, resolver behavior, payload shapes, and error formatting.
- Full E2E/client tests: planned later, not part of the immediate implementation scope.

## E2E Status
E2E coverage is planned later.

Notes for later:
- keep the current `docs/e2e-quickstart.md` guide as the starting point
- do not let E2E planning block unit and integration implementation
- when E2E work starts, reuse the auth, dashboard, and isolation scenarios already documented below

## Tooling
- Test framework: `xUnit.net v3`
- Mocking: `Moq` (or `NSubstitute` if preferred, but pick one)
- Assertions: built-in xUnit assertions by default
- DB integration:
  - Required: Testcontainers PostgreSQL for automated database-backed tests

### Recommended Package Baseline
For the initial test projects, use:

- `Microsoft.NET.Test.Sdk` `18.4.0`
- `xunit.v3` `3.2.2`
- `xunit.runner.visualstudio` `3.1.5`
- `Moq` `4.20.72`

Optional:

- `FluentAssertions` `8.9.0`

Why this baseline:
- xUnit v3 keeps the project on the latest stable xUnit line
- `xunit.runner.visualstudio` keeps local IDE and `dotnet test` discovery straightforward
- `Microsoft.NET.Test.Sdk` stays current with modern test-host tooling
- `Moq` is enough for service-layer isolation without overcomplicating setup
- built-in xUnit assertions keep the first test phase consistent and lightweight

---

## Test Identification and Traceability

Every meaningful test should be easy to track in logs, CI output, PR discussion, and failure triage.

### Required Test Identity Pattern

Each test should carry a unique case identifier in the method name, display name, or both.

Recommended format:

```text
UT-<FEATURE>-<AREA>-<NUMBER>
IT-<FEATURE>-<AREA>-<NUMBER>
```

Examples:
- `UT-AUTH-SIGNIN-001`
- `UT-DASHBOARD-CACHE-003`
- `IT-DASHBOARD-REPOSITORY-005`
- `IT-LOAN-REPAYMENT-002`

### Naming Standard

Use test names that include:
- the unique identifier
- the scenario
- the expected outcome

Recommended style:

```text
UT_DASHBOARD_CACHE_001_ReturnsCachedSummary_WhenCacheHit
IT_LOAN_REPOSITORY_002_DeletesOnlyCurrentUsersLoan
```

### xUnit Display Names

Use xUnit display names for readability in test runners and CI output.

Recommended pattern:

```text
[Fact(DisplayName = "UT-DASHBOARD-CACHE-001 returns cached summary when cache hit occurs")]
```

### Traits

Use traits for consistent filtering and reporting:
- `Suite`: `Unit` or `Integration`
- `Feature`: `Auth`, `Dashboard`, `Income`, `Expense`, `Savings`, `Loan`
- `CaseId`: unique test identifier

This makes it easier to filter failures and build reports locally and in CI.

---

## Failure Diagnostics Standard

When a test fails, the output should make it obvious:
- which test failed
- what business rule or repository behavior was expected
- what actual result was observed
- what data setup or identifier is relevant

### Required Failure Clarity

Tests should be written so that failures answer:
- what was the expected outcome?
- what was the actual outcome?
- what scenario was under test?
- which case ID does this correspond to?

### Recommended Practices

- use clear arrange/act/assert structure
- prefer one behavioral responsibility per test
- avoid giant test methods with multiple unrelated assertions
- include explicit assertion messages where the failure could be ambiguous
- log seeded IDs, dates, and user identifiers when integration setup matters

### For Unit Tests

Prefer:
- deterministic inputs
- small mocks
- exact verification of important interactions

### For Integration Tests

Log enough context to debug quickly:
- current user id
- seeded entity ids
- filter values
- relevant timestamps

Use test output helpers or shared test logging utilities where needed.

---

## Console Output and Tracking

Test execution should be easy to follow in the console.

### Local Run Expectations

Local test runs should provide:
- visible test names
- clear pass/fail status
- failure stack traces
- enough verbosity to identify the failed case ID quickly

Recommended commands:

```bash
dotnet test Tests/UnitTests/UnitTests.csproj --logger "console;verbosity=detailed" --logger "trx;LogFileName=unit-tests.trx"
dotnet test Tests/IntegrationTests/IntegrationTests.csproj --logger "console;verbosity=detailed" --logger "trx;LogFileName=integration-tests.trx"
```

Why:
- detailed console logs make failures easier to trace immediately
- TRX output gives machine-readable results for CI and artifact review

### Tracking Standard

During execution, a developer should be able to tell:
- which suite is running
- which feature area is running
- which specific test case failed
- whether the issue is validation, data setup, repository behavior, or cache behavior

---

## End-of-Run Summary Standard

When the tests finish, the output should provide a clear summary that is easy to scan.

### Required Summary Information

At minimum, the final summary should show:
- total tests run
- passed count
- failed count
- skipped count
- duration
- failed test case identifiers
- suite-level grouping when possible

### Recommended Reporting Shape

Use:
- console summary for immediate developer feedback
- TRX artifacts for CI storage and machine-readable results
- optional HTML summary later for richer CI presentation

### CI-Oriented Summary Expectations

When CI is added, the workflow should surface:
- build result
- unit test result
- integration test result
- failed test identifiers
- direct links to logs or artifacts

This should feel obvious and actionable, similar to strong enterprise CI pipelines.

For CI/deployment workflow design, see:
- [ci-plan.md](/workspaces/finance-tracker-graphql/docs/ci-plan.md)

## Proposed Test Project Layout
Create a top-level `Tests/` directory with two projects only for now:

```
Tests/
  UnitTests/
  IntegrationTests/
```

Within each project, mirror the app feature areas in a clean, independent way.

```
Tests/
  UnitTests/
    Auth/
    Dashboard/
    Expense/
    Income/
    Loan/
    Savings/
    Common/
    Builders/
  IntegrationTests/
    Caching/
    Dashboard/
    Database/
    Expense/
    Fixtures/
    Income/
    Loan/
    Savings/
```

### Folder Intent
- `Auth/`: auth service and auth-contract-related test suites
- `Dashboard/`: dashboard service, repository, and cache behavior suites
- `Income/`, `Expense/`, `Savings/`, `Loan/`: feature-specific suites
- `Common/`: shared assertion helpers, constants, and test utilities
- `Builders/`: deterministic test-data builders for unit tests
- `Fixtures/`: database fixtures, seeded contexts, and shared integration setup
- `Database/`: lower-level DB or schema behavior checks
- `Caching/`: Redis/distributed cache integration coverage

Shared naming, traits, and output rules live in:
- [testing-conventions.md](/workspaces/finance-tracker-graphql/docs/testing-conventions.md)

## Test Data Strategy
- Prefer builders/factories for inputs and entities (avoid copy/paste).
- Use fixed timestamps in tests (no `DateTime.UtcNow` comparisons without control).
- Use deterministic GUIDs where helpful for readability.
- Seed minimal data for each test; clean up after each test.
- Keep every test independent; do not rely on test execution order.

Recommended helpers (to add as tests grow):
- `TestUser` builder (two users A/B for isolation tests).
- `IncomeBuilder`, `ExpenseBuilder`, `LoanBuilder`, `SavingsGoalBuilder`.
- `DashboardSummaryBuilder` only if needed for service-level cache tests.
- `FixedDate` or `TestClock` helper for time-sensitive assertions

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
  - Optional phone number persists when provided
- Update:
  - Counterparty empty -> fail
  - DueDate <= Date -> fail
  - Amount < amount already repaid -> fail
- RecordRepayment:
  - Amount <= 0 -> fail
  - Amount > remaining -> fail
  - Repayment date < loan date -> fail
  - Valid repayment -> ok
- GetRepaymentHistory:
  - Missing loan / wrong user -> fail
  - Existing loan -> ordered repayment list returned
- Settle:
  - Already settled loan -> fail
  - Active loan -> ok, records final repayment event
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
  - Valid -> returns API access token via `IApiTokenService`
  - Valid -> preserves refresh token from `ISupabaseAuthClient`

### DashboardService
- Cache hit:
  - returns cached dashboard
  - does not call repository methods
- Cache miss:
  - reads totals, savings goals, loan summary, and recent activity from the repository
  - stores the rebuilt dashboard in cache
- Activity limit normalization:
  - `null` -> default
  - `<= 0` -> default
  - `> max` -> capped
- Summary composition:
  - `netBalance` = income - expenses
  - `expenseToIncomeRatio` rules are correct
  - `hasNegativeNetBalance` rules are correct
  - `hasOverdueLoans` reflects the loan summary
  - `focusGoal` selection is stable for mixed savings-goal data

### Cache Invalidation Unit Coverage
These service tests should verify `InvalidateUserAsync` is called after successful writes:

- `IncomeService`
  - add
  - update
  - delete success
- `ExpenseService`
  - add
  - update
  - delete success
- `SavingsGoalService`
  - create
  - update
  - add contribution
  - update progress
  - delete success
- `LoanService`
  - add
  - update
  - record repayment
  - settle
  - delete success

And should verify invalidation is *not* called on failed operations where no write succeeds.

## Integration Test Plan (Infrastructure + DB)
Target: repositories and EF mappings.

## Integration Test Scope
Integration tests in this codebase should focus on three areas:

- repository correctness against a real Postgres database
- dashboard repository aggregation correctness
- cache implementation behavior with distributed cache abstractions

Database reset strategy decision:
- use **database reset / truncate between tests or test groups**
- do not rely on test execution order
- avoid hidden shared state

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
- `counterparty_phone_number` persists and round-trips correctly.
- `loan_repayments` history persists with correct ordering and cascade delete behavior.
- Loan status filtering behaves correctly for `ACTIVE`, `SETTLED`, and `OVERDUE`.
- PostgreSQL enum mapping for `LoanType` round-trips correctly (`OWED` / `GIVEN` at GraphQL level).

### Timeout/Resilience
- Simulated DB delay surfaces `REQUEST_TIMEOUT` (or payload `errors`) with guidance to refresh before retrying.

### DB Environment
Required: Testcontainers PostgreSQL per test run.

Why:
- hermetic and repeatable
- no real database credentials required
- disposable lifecycle
- safe for destructive reset
- same standard locally and in CI

External databases are not supported for automated integration tests.

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

### Dashboard Repository Integration Tests
- `GetTotalIncomeAsync` respects user scoping and date filters
- `GetTotalExpensesAsync` respects user scoping and date filters
- `GetSavingsGoalsAsync` returns goals ordered by `CreatedAt` descending
- `GetLoanSummaryAsync` calculates:
  - `TotalOwed`
  - `TotalGiven`
  - `ActiveLoanCount`
  - `OverdueLoanCount`
  - `SettledLoanCount`
- `GetRecentActivityAsync`:
  - merges income, expense, savings, loan, and repayment activity
  - returns results in descending `HappenedAt` order
  - respects `activityLimit`
  - respects `fromDate` / `toDate`
  - scopes joined loan-repayment activity to the current user

### Dashboard Cache Integration Tests
- `DashboardCacheService.GetAsync` returns `null` on miss
- `DashboardCacheService.SetAsync` stores a dashboard and `GetAsync` returns it
- cache keys vary by:
  - `userId`
  - `fromDate`
  - `toDate`
  - `activityLimit`
- `InvalidateUserAsync` causes subsequent reads to miss old dashboard entries
- disabled cache mode bypasses cache reads and writes cleanly
- invalid payload handling removes unreadable cached entries safely

### EF Mapping Tests (High Value)
- `created_at` and `user_id` are not rewritten on update (regression guard).
- `numeric(18,2)` precision is preserved for amounts.

## Deferred API / E2E Coverage
The scenarios below are still valuable, but they are intentionally deferred until after the unit and integration layers are in place.

## Coverage Matrix (What We Test Where)
This matrix helps prevent gaps and prevents duplicating work in the wrong layer.

| Area | Unit (Services) | Integration (Repos/DB) | API (GraphQL/HTTP) |
|---|---|---|---|
| Income | ✅ validations, not-found, delete result | ✅ CRUD + scoping + delete bool | ✅ auth + payload + isolation |
| Expense | ✅ validations, not-found, delete result | ✅ CRUD + scoping + delete bool | ✅ auth + payload + isolation |
| Savings | ✅ create/update/progress rules | ✅ CRUD + progress persistence | ✅ auth + payload + isolation |
| Loans | ✅ due date + repayment + settlement rules | ✅ CRUD + repayment history + enum mapping | ✅ auth + payload + isolation + status filters |
| Auth | ✅ input validation + token issuance | ⚠️ minimal (mock supabase) | ✅ reject missing/expired token |
| Dashboard | ✅ service composition + cache behavior | ✅ repository + cache verification | ⏳ later |
| Error handling | ✅ service error messages | ✅ DB errors mapped correctly | ✅ timeout/auth mapped consistently |

Legend:
- ✅ required for “done”
- ⚠️ keep thin to avoid flaky external calls

### Later API Auth Enforcement
For every protected query/mutation:
- No JWT -> 401 (or GraphQL auth error mapped to 401)
- Expired JWT -> 401
- Valid JWT -> 200 and operation succeeds

### Later API Payload Contract (Mutations)
For add/update/delete mutations:
- On success:
  - payload entity is not null (except deletes)
  - `errors` is null
  - `message` is a user-friendly success string
- On validation failure:
  - entity is null
  - `errors` contains the message
  - `message` is null

### Auth Response Contract
- `POST /api/auth/signin` success payload contains:
  - `accessToken`
  - `refreshToken`
  - `tokenType`
- `accessToken` must be the API-issued JWT used for GraphQL authorization.
- `refreshToken` must not be blank when the upstream auth provider returns one.
- Until a refresh endpoint exists, tests should verify the field is present but should not assume `/api/auth/refresh` behavior.

### Later API Isolation (Critical)
For each entity type:
- User A creates data
- User B cannot read it (`GetAll` doesn’t include it; `GetById` returns null)
- User B cannot update/delete it (returns “not found” or equivalent)

### Later API Error/Timeout Scenarios (Regression Guards)
- When DB operation times out or is canceled:
  - API surfaces `REQUEST_TIMEOUT` (or payload `errors` for update paths if handled there)
  - error contains guidance to refresh/check state before retry

## Suggested File Blueprint
Use small, focused test files instead of huge feature dumps.

Example unit-test file layout:

```text
Tests/UnitTests/
  Auth/
    AuthServiceSignInTests.cs
    AuthServiceSignUpTests.cs
  Dashboard/
    DashboardServiceCacheTests.cs
    DashboardServiceSummaryTests.cs
  Expense/
    ExpenseServiceAddTests.cs
    ExpenseServiceUpdateTests.cs
    ExpenseServiceDeleteTests.cs
  Income/
    IncomeServiceAddTests.cs
    IncomeServiceUpdateTests.cs
    IncomeServiceDeleteTests.cs
  Loan/
    LoanServiceAddTests.cs
    LoanServiceUpdateTests.cs
    LoanServiceRepaymentTests.cs
    LoanServiceSettlementTests.cs
    LoanServiceDeleteTests.cs
  Savings/
    SavingsGoalServiceCreateTests.cs
    SavingsGoalServiceUpdateTests.cs
    SavingsGoalServiceContributionTests.cs
    SavingsGoalServiceProgressTests.cs
    SavingsGoalServiceDeleteTests.cs
```

Example integration-test file layout:

```text
Tests/IntegrationTests/
  Caching/
    DashboardCacheServiceTests.cs
  Dashboard/
    DashboardRepositoryTotalsTests.cs
    DashboardRepositoryActivityTests.cs
    DashboardRepositoryLoanSummaryTests.cs
  Expense/
    ExpenseRepositoryTests.cs
  Income/
    IncomeRepositoryTests.cs
  Loan/
    LoanRepositoryTests.cs
    LoanRepaymentRepositoryTests.cs
  Savings/
    SavingsGoalRepositoryTests.cs
  Fixtures/
    PostgresFixture.cs
    TestDatabaseReset.cs
```

## Manual Smoke Tests (Docs/Postman)
For quick human verification and demos, keep the Postman examples current:
- `docs/postman/test-examples.json`

These are not a substitute for automated tests, but they are useful for:
- sanity-checking deployments
- verifying auth/token flows
- reproducing issues quickly

## Test Execution (Local)
Proposed commands (once the projects exist):
- `dotnet test Tests/UnitTests/UnitTests.csproj`
- `dotnet test Tests/IntegrationTests/IntegrationTests.csproj`

### Test Configuration (Recommended)
Prefer injecting config in tests rather than relying on developer machine settings:
- `Auth:JwtSecret`: fixed test value (>= 32 chars).
- `ConnectionStrings:Default`: points to local Postgres container.
- `Supabase:*`: can be dummy values for most tests (auth client is mocked in unit tests).

## CI Recommendations
You do **not** need a `ci.yml` file to begin the unit and integration test setup.

Start with:
- the test project structure
- package references
- fixtures/builders
- local `dotnet test` execution

Add a CI workflow only after the local test suites are stable enough to automate confidently.

### When `ci.yml` becomes necessary
Add a GitHub Actions workflow such as `.github/workflows/ci.yml` when you want:
- automatic test runs on pull requests
- a consistent team-level verification path
- protection against regressions before merge
- structured summaries and downloadable test artifacts

### What is not required right now
- `ci.yml`
- E2E workflow files
- deployment/test orchestration scripts beyond local test commands

### What is required right now
- `Tests/UnitTests/`
- `Tests/IntegrationTests/`
- clean project references and package setup
- deterministic local test execution
- readable detailed console output
- stable result files such as TRX for later CI reporting
- a CI workflow once deployment gating begins

- Run unit tests on every PR.
- Run integration tests on PRs that touch `Infrastructure/`, `Api/`, or migrations/scripts.
- Add API/E2E jobs later as a separate workstream.
- Cache NuGet packages.
- Use a Postgres service container for integration tests (preferred).

### CI Outline (Example)
High-level workflow:
- Restore/build
- Unit tests
- Start Postgres service
- Apply schema/migrations/seed
- Integration tests

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
- Keep test classes small and feature-focused.
- Prefer one responsibility per test file.
- Shared fixtures should live under `Fixtures/`, not inside feature folders.

## Exit Criteria
- All tests pass locally and in CI.
- No cross-user data leakage in any feature.
- Dashboard cache behavior is covered at unit and integration levels.
- Auth sign-in contract is covered for `accessToken`, `refreshToken`, and `tokenType`.
- A contributor can run the unit or integration suites independently with deterministic results.
