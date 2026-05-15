# Integration Test Plan

## Purpose

This document defines the integration test strategy for the Vela backend.

Integration tests in this codebase should validate that infrastructure components behave correctly with real dependencies, especially:
- PostgreSQL
- Entity Framework Core mappings
- repositories
- dashboard aggregation queries
- distributed cache implementation behavior

Environment and database safety requirements are defined in:
- [test-requirements.md](/workspaces/vela/docs/test-requirements.md)

---

## Scope

Integration tests cover:
- repository correctness against a real database
- user scoping at the persistence layer
- dashboard repository aggregate behavior
- dashboard cache service behavior with distributed cache abstractions

Integration tests do **not** cover:
- full REST request pipeline in the repository-focused suite
- REST controller status-code behavior
- full browser/client journeys

REST pipeline coverage should be added as API integration tests. Those tests may live in a separate `Tests/ApiTests` project or under `Tests/IntegrationTests/Api`, but they should still use an isolated test database.

---

## Project Structure

Use:

```text
Tests/
  IntegrationTests/
```

Recommended layout:

```text
Tests/IntegrationTests/
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

- `Income/`, `Expense/`, `Savings/`, `Loan/`: repository tests by feature
- `Dashboard/`: dashboard repository aggregate and recent-activity tests
- `Caching/`: `DashboardCacheService` tests
- `Database/`: schema/mapping/regression checks
- `Fixtures/`: shared database and cache setup

---

## Package Baseline

Recommended packages:

- `Microsoft.NET.Test.Sdk` `18.4.0`
- `xunit.v3` `3.2.2`
- `xunit.runner.visualstudio` `3.1.5`
- `Npgsql`
- `Microsoft.EntityFrameworkCore`

Recommended infrastructure helpers:

- `DotNet.Testcontainers` or PostgreSQL-specific Testcontainers package

Optional:

- `FluentAssertions` `8.9.0`

Assertion style decision for the initial integration-test phase:
- use built-in xUnit assertions by default
- keep assertion style consistent across repository and cache suites

---

## Framework Standard

Integration tests in this codebase should explicitly use **xUnit.net v3**.

This is the standard test framework for the integration layer as well.

All examples, naming conventions, and reporting guidance in this plan assume xUnit.

---

## Environment Strategy

Required setup:
- Testcontainers PostgreSQL
- real PostgreSQL test database, created only for the test run
- disposable database lifecycle
- deterministic seed data
- Respawn reset before each repository test

The only supported database path for automated integration tests is Testcontainers PostgreSQL. Development happens inside devcontainers, so the devcontainer must expose Docker/Testcontainers support. If Docker/Testcontainers is unavailable in the devcontainer or CI, that is an environment setup failure, not a reason to use another database path.

Repository SQL alignment rules:
- project SQL files stay under [scripts](/workspaces/vela/scripts)
- automated tests may read SQL assets from `scripts/` if needed
- `.devcontainer/` is for environment setup, not duplicate schema ownership
- automated integration tests do not rely on a long-lived Postgres container managed separately from the test run

Never run integration reset logic against:
- production
- staging
- shared development databases
- dedicated developer databases
- any Supabase project that contains real user data

Cache coverage:
- use in-memory distributed cache for core behavior verification when appropriate
- add Redis-backed verification if the environment is available and stable

Database reset strategy decision:
- use explicit database reset / truncate between tests or test groups
- do not depend on transaction magic or execution order as the primary reset mechanism
- keep reset logic centralized in shared fixtures

---

## Design Rules

- every test should prepare only the data it needs
- no shared mutable state across tests
- reset database state between tests
- keep fixtures explicit and small
- prefer feature-based test classes over giant catch-all files
- keep database reset logic in shared fixtures, not duplicated in feature tests

Shared conventions:
- [testing-conventions.md](/workspaces/vela/docs/testing-conventions.md)

## Integration Test Naming and Identity Standard

Each integration test should have:
- a unique case identifier
- a readable xUnit display name
- traceable setup context

Recommended identifier format:

```text
IT-<FEATURE>-<AREA>-<NUMBER>
```

Examples:
- `IT-DASHBOARD-REPOSITORY-001`
- `IT-CACHE-DASHBOARD-003`
- `IT-LOAN-REPAYMENT-002`

Recommended method naming style:

```text
IT_DASHBOARD_REPOSITORY_001_ReturnsFilteredIncomeTotal_ForCurrentUser
```

Recommended xUnit usage:

```text
[Fact(DisplayName = "IT-DASHBOARD-REPOSITORY-001 returns filtered income total for current user")]
```

Use traits where helpful:
- `Suite=Integration`
- `Feature=Dashboard`
- `CaseId=IT-DASHBOARD-REPOSITORY-001`

---

## Failure Clarity Standard

Integration test failures should be diagnosable without guesswork.

When an integration test fails, the output should make it obvious:
- which data was seeded
- which user scope was used
- which identifiers were involved
- which aggregate or persistence rule failed

Recommended logging context:
- user ids
- seeded entity ids
- relevant dates
- relevant amounts
- relevant filter values

Use shared output helpers when setup becomes non-trivial.

---

## Console and Output Standard

Integration test runs should be verbose enough to help with triage, especially for repository and cache behavior.

Recommended command:

```bash
dotnet test Tests/IntegrationTests/IntegrationTests.csproj --logger "console;verbosity=detailed" --logger "trx;LogFileName=integration-tests.trx"
```

Expected outcome:
- failures point to exact feature areas and case identifiers
- database-related failures are easy to distinguish from cache-related failures
- TRX output is available for CI summary and artifact publishing later

---

## Core Coverage

### Repository Coverage

For each repository:
- `GetAllAsync` returns only the current user’s data
- `GetByIdAsync` respects both `id` and `userId`
- `AddAsync` persists correct values
- `UpdateAsync` updates intended mutable fields only
- `DeleteAsync`:
  - returns `false` when the row is missing or belongs to another user
  - returns `true` when deletion succeeds

### Income Repository

Test areas:
- user scoping
- date ordering
- `AsNoTracking` read path does not break returned results

### Expense Repository

Test areas:
- user scoping
- date ordering
- update and delete correctness

### Savings Goal Repository

Test areas:
- user scoping
- ordering by `CreatedAt`
- progress-related persistence correctness

### Loan Repository

Test areas:
- user scoping
- enum mapping correctness for `LoanType`
- due date persistence
- amount repaid persistence
- status-related repository reads where applicable

### Loan Repayment Repository

Test areas:
- repayment history ordering
- persistence of note/date/amount
- user isolation through associated loan ownership

---

## Dashboard Repository Coverage

Target:
- `DashboardRepository`

Test areas:
- `GetTotalIncomeAsync`
  - sums only the current user’s income
  - respects `fromDate` / `toDate`
- `GetTotalExpensesAsync`
  - sums only the current user’s expenses
  - respects `fromDate` / `toDate`
- `GetSavingsGoalsAsync`
  - returns only the current user’s goals
  - orders by `CreatedAt` descending
- `GetLoanSummaryAsync`
  - calculates `TotalOwed`
  - calculates `TotalGiven`
  - calculates `ActiveLoanCount`
  - calculates `OverdueLoanCount`
  - calculates `SettledLoanCount`
- `GetRecentActivityAsync`
  - merges income, expense, savings goal, loan, and repayment activity
  - respects `activityLimit`
  - respects `fromDate` / `toDate`
  - keeps activity ordered by `HappenedAt` descending
  - scopes loan-repayment activity to the current user

---

## Dashboard Cache Coverage

Target:
- `DashboardCacheService`

Test areas:
- cache miss returns `null`
- set then get returns the stored dashboard
- keys vary by:
  - `userId`
  - `fromDate`
  - `toDate`
  - `activityLimit`
- `InvalidateUserAsync` causes subsequent reads to miss old entries
- disabled cache mode bypasses reads and writes
- corrupted cached payload is removed safely
- version token logic isolates old dashboard entries after invalidation

---

## Database Regression Coverage

High-value checks:
- `created_at` is not unintentionally rewritten on update
- `user_id` is not unintentionally rewritten on update
- decimal precision remains correct for finance amounts
- foreign-key behavior for repayments and loans remains valid

---

## Suggested File Blueprint

```text
Tests/IntegrationTests/
  Caching/
    DashboardCacheServiceTests.cs
  Dashboard/
    DashboardRepositoryActivityTests.cs
    DashboardRepositoryLoanSummaryTests.cs
    DashboardRepositoryTotalsTests.cs
  Database/
    DecimalPrecisionTests.cs
    ImmutableFieldRegressionTests.cs
  Expense/
    ExpenseRepositoryTests.cs
  Fixtures/
    PostgresFixture.cs
    TestDatabaseReset.cs
  Income/
    IncomeRepositoryTests.cs
  Loan/
    LoanRepaymentRepositoryTests.cs
    LoanRepositoryTests.cs
  Savings/
    SavingsGoalRepositoryTests.cs
```

---

## Fixture Strategy

Recommended shared fixtures:
- `PostgresFixture`
  - starts or connects to the test database
  - provides connection/configuration
- `TestDatabaseReset`
  - clears and reseeds data between tests

Optional shared helpers:
- `SeedDataFactory`
- `IntegrationTestUsers`
- `CacheTestDataFactory`

Keep feature seed helpers near fixtures, not embedded repeatedly in test files.

---

## End-of-Run Summary Expectations

At the end of an integration run, the summary should clearly show:
- total integration tests executed
- passed / failed / skipped counts
- duration
- failed case identifiers
- failed feature groups such as `Dashboard`, `Caching`, or `Loan`

This should support quick triage before deployment or merge.

---

## Local Execution

Expected commands once the project exists:

```bash
dotnet test Tests/IntegrationTests/IntegrationTests.csproj
```

Prerequisites:
- PostgreSQL available locally or through containers
- deterministic test configuration

---

## Exit Criteria

- repository CRUD and scoping behavior is covered
- dashboard repository aggregation is covered
- dashboard cache behavior is covered
- database regression checks for key finance behaviors are covered
- tests are isolated, repeatable, and safe to run in CI
