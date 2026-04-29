# Unit Test Plan

## Purpose

This document defines the unit test strategy for the Finance Tracker backend.

Unit tests in this codebase should verify business rules, service orchestration, and small pure helpers without touching a real database, Redis, HTTP, or GraphQL runtime behavior.

---

## Scope

Unit tests cover:
- application services
- validation rules
- result handling
- dashboard summary composition
- cache hit / miss orchestration
- cache invalidation decisions after successful writes

Unit tests do **not** cover:
- real database behavior
- repository SQL correctness
- Redis connectivity
- GraphQL resolver pipeline
- HTTP status codes

---

## Project Structure

Use:

```text
Tests/
  UnitTests/
```

Recommended feature layout:

```text
Tests/UnitTests/
  Auth/
  Common/
  Dashboard/
  Expense/
  Income/
  Loan/
  Savings/
  Builders/
```

### Folder Intent

- `Auth/`: `AuthService` tests
- `Dashboard/`: `DashboardService` tests
- `Income/`, `Expense/`, `Savings/`, `Loan/`: feature service tests
- `Common/`: shared assertions, constants, fake users, fixed dates
- `Builders/`: deterministic test data builders

---

## Package Baseline

Recommended packages:

- `Microsoft.NET.Test.Sdk` `18.4.0`
- `xunit.v3` `3.2.2`
- `xunit.runner.visualstudio` `3.1.5`
- `Moq` `4.20.72`

Optional:

- `FluentAssertions` `8.9.0`

Assertion style decision for the initial unit-test phase:
- use built-in xUnit assertions by default
- do not mix assertion styles randomly across the same feature area
- reconsider `FluentAssertions` only if the team adopts it consistently

---

## Framework Standard

Unit tests in this codebase should explicitly use **xUnit.net v3**.

This is not just one acceptable option; it is the team standard for unit testing.

All examples, naming conventions, and reporting expectations in this document assume xUnit.

---

## Design Rules

- each test file should focus on one service behavior area
- no real network or database calls
- all collaborators should be mocked or replaced with test doubles
- use fixed timestamps and deterministic GUIDs
- keep tests independent and order-agnostic
- keep shared helpers under `Builders/` or `Common/`, not hidden inside individual test files

Shared conventions:
- [TESTING-CONVENTIONS.md](/workspaces/finance-tracker-graphql/docs/TESTING-CONVENTIONS.md)

## Unit Test Naming and Identity Standard

Each unit test should have:
- a unique case identifier
- a readable xUnit display name
- a single clear scenario

Recommended identifier format:

```text
UT-<FEATURE>-<AREA>-<NUMBER>
```

Examples:
- `UT-AUTH-SIGNIN-001`
- `UT-DASHBOARD-CACHE-002`
- `UT-INCOME-ADD-003`

Recommended method naming style:

```text
UT_DASHBOARD_CACHE_001_ReturnsCachedSummary_WhenCacheHit
```

Recommended xUnit usage:

```text
[Fact(DisplayName = "UT-DASHBOARD-CACHE-001 returns cached summary when cache hit occurs")]
```

Use traits where helpful:
- `Suite=Unit`
- `Feature=Dashboard`
- `CaseId=UT-DASHBOARD-CACHE-001`

---

## Failure Clarity Standard

Unit-test failures should make the business rule obvious.

A failing unit test should tell the reader:
- which rule failed
- what input was used
- what result was expected
- what result was actually returned

Recommended patterns:
- explicit assertion messages when ambiguity is possible
- small arrange sections with deterministic values
- one reason to fail per test
- verify both returned result and important mock interactions when needed

---

## Console and Output Standard

Unit test runs should be easy to read locally.

Recommended command:

```bash
dotnet test Tests/UnitTests/UnitTests.csproj --logger "console;verbosity=detailed" --logger "trx;LogFileName=unit-tests.trx"
```

Expected outcome:
- each test is traceable by case identifier
- failures are easy to map back to a feature and business rule
- TRX results are available for CI or artifact review later

Recommended local command:

```bash
dotnet test Tests/UnitTests/UnitTests.csproj --logger "console;verbosity=detailed" --logger "trx;LogFileName=unit-tests.trx"
```

---

## Core Coverage

### Auth

Target:
- `AuthService`

Test areas:
- invalid email fails
- short password fails on sign-up
- missing password fails on sign-in
- auth client failure is returned cleanly
- valid sign-in returns:
  - API access token
  - refresh token preserved from upstream auth client
  - `tokenType`

### Income

Target:
- `IncomeService`

Test areas:
- add validation:
  - amount must be greater than 0
  - source required
  - date cannot be in the future
- update:
  - missing record fails
  - valid update succeeds
- delete:
  - missing record fails
  - successful delete returns ok
- cache invalidation:
  - add success invalidates dashboard cache
  - update success invalidates dashboard cache
  - delete success invalidates dashboard cache
  - failed delete does not invalidate cache

### Expense

Target:
- `ExpenseService`

Test areas:
- add validation:
  - amount must be greater than 0
  - category required
  - date cannot be in the future
- update:
  - missing record fails
  - valid update succeeds
- delete:
  - missing record fails
  - successful delete returns ok
- cache invalidation:
  - add success invalidates dashboard cache
  - update success invalidates dashboard cache
  - delete success invalidates dashboard cache
  - failed delete does not invalidate cache

### Savings

Target:
- `SavingsGoalService`

Test areas:
- create:
  - name required
  - target amount must be greater than 0
- update:
  - target cannot be less than current amount
- add contribution:
  - amount must be greater than 0
  - contribution cannot exceed target
- update progress:
  - amount cannot be negative
  - amount cannot exceed target
- delete:
  - missing record fails
  - successful delete returns ok
- cache invalidation after successful writes

### Loans

Target:
- `LoanService`

Test areas:
- add:
  - counterparty required
  - amount must be greater than 0
  - due date must be after loan date
- update:
  - amount cannot be below amount already repaid
  - due date validation
- record repayment:
  - amount must be greater than 0
  - amount cannot exceed remaining balance
  - repayment date cannot be before loan date
- settle:
  - already settled loan fails
  - valid settlement records repayment
- delete:
  - missing record fails
  - successful delete returns ok
- cache invalidation after successful writes

### Dashboard

Target:
- `DashboardService`

Test areas:
- cache hit returns cached dashboard and skips repository reads
- cache miss rebuilds from repository and stores result
- activity limit normalization:
  - `null`
  - `<= 0`
  - above max
- summary composition:
  - `netBalance`
  - `expenseToIncomeRatio`
  - `hasNegativeNetBalance`
  - `hasOverdueLoans`
- savings overview:
  - total goals
  - completed goals
  - focus goal selection
- recent activity passed through correctly

---

## Suggested File Blueprint

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
    ExpenseServiceDeleteTests.cs
    ExpenseServiceUpdateTests.cs
  Income/
    IncomeServiceAddTests.cs
    IncomeServiceDeleteTests.cs
    IncomeServiceUpdateTests.cs
  Loan/
    LoanServiceAddTests.cs
    LoanServiceDeleteTests.cs
    LoanServiceRepaymentTests.cs
    LoanServiceSettlementTests.cs
    LoanServiceUpdateTests.cs
  Savings/
    SavingsGoalServiceContributionTests.cs
    SavingsGoalServiceCreateTests.cs
    SavingsGoalServiceDeleteTests.cs
    SavingsGoalServiceProgressTests.cs
    SavingsGoalServiceUpdateTests.cs
  Builders/
    ExpenseBuilder.cs
    IncomeBuilder.cs
    LoanBuilder.cs
    SavingsGoalBuilder.cs
    TestUserBuilder.cs
```

---

## Mocking Strategy

Use mocks for:
- repository interfaces
- `IApiTokenService`
- `ISupabaseAuthClient`
- `IDashboardCacheService`
- `IDashboardRepository`

Prefer verifying:
- returned `AppResult`
- expected repository/cache interactions
- expected non-interactions on failure paths

---

## Shared Unit-Test Infrastructure

The unit-test layer should establish shared infrastructure early.

Recommended shared helpers:
- `Builders/TestUserBuilder.cs`
- `Builders/IncomeBuilder.cs`
- `Builders/ExpenseBuilder.cs`
- `Builders/LoanBuilder.cs`
- `Builders/SavingsGoalBuilder.cs`
- `Common/FixedDate.cs`
- `Common/TestConstants.cs`

This helps prevent repetitive arrange code and keeps tests small.

---

## End-of-Run Summary Expectations

At the end of a unit test run, the developer should have a clear view of:
- total unit tests executed
- passed / failed / skipped counts
- duration
- failed case identifiers
- failed feature areas

The console should be readable first.

TRX output should act as the structured result source for CI and future reporting.

---

## Exit Criteria

- all service validation rules are covered
- dashboard cache hit/miss orchestration is covered
- dashboard cache invalidation behavior is covered for successful writes
- auth sign-in response contract is covered
- tests are deterministic and independent
