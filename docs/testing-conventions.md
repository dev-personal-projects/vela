# Testing Conventions

## Purpose

This document defines the shared testing conventions for the Finance Tracker backend.

It exists to keep:
- unit tests
- integration tests
- future API/E2E tests

consistent in naming, structure, diagnostics, and reporting.

Related plans:
- [test-plan.md](/workspaces/vela/docs/test-plan.md)
- [unit-test-plan.md](/workspaces/vela/docs/unit-test-plan.md)
- [integration-test-plan.md](/workspaces/vela/docs/integration-test-plan.md)

---

## Framework Standard

The test framework standard for this codebase is **xUnit.net v3**.

Use xUnit for:
- unit tests
- integration tests
- future API/E2E tests unless there is a deliberate exception

Supporting tools:
- `Microsoft.NET.Test.Sdk`
- `xunit.runner.visualstudio`
- `Moq`

---

## Assertion Standard

Primary assertion style:
- use built-in **xUnit assertions** by default

Optional secondary style:
- `FluentAssertions` may be added later, but it should not create a mixed assertion style within the same feature area

Current recommendation:
- begin with xUnit assertions only
- introduce `FluentAssertions` only if the team decides to standardize on it consistently

Why:
- lower complexity
- no ambiguity for contributors
- easier early consistency

---

## Naming Standard

Every meaningful test should have:
- a unique case identifier
- a readable method name
- a readable display name

### Case Identifier Format

Use:

```text
UT-<FEATURE>-<AREA>-<NUMBER>
IT-<FEATURE>-<AREA>-<NUMBER>
```

Examples:
- `UT-AUTH-SIGNIN-001`
- `UT-DASHBOARD-CACHE-002`
- `IT-DASHBOARD-REPOSITORY-003`
- `IT-LOAN-REPAYMENT-004`

### Method Name Format

Recommended format:

```text
UT_DASHBOARD_CACHE_001_ReturnsCachedSummary_WhenCacheHit
IT_LOAN_REPOSITORY_002_DeletesOnlyCurrentUsersLoan
```

### xUnit Display Name Format

Recommended usage:

```csharp
[Fact(DisplayName = "UT-DASHBOARD-CACHE-001 returns cached summary when cache hit occurs")]
```

---

## Trait Standard

Use xUnit traits to support filtering and reporting.

Recommended traits:
- `Suite`
- `Feature`
- `CaseId`

Examples:
- `Suite=Unit`
- `Suite=Integration`
- `Feature=Dashboard`
- `Feature=Loan`
- `CaseId=UT-DASHBOARD-CACHE-001`

This makes it easier to:
- filter test runs
- group failures in CI
- trace failures back to docs and plans

---

## Test File Structure Standard

Prefer:
- one behavioral area per file

Avoid:
- giant “all tests for a service” files if they grow too large

Examples:
- `AuthServiceSignInTests.cs`
- `DashboardServiceCacheTests.cs`
- `LoanServiceSettlementTests.cs`

This keeps failures local and test files easier to review.

---

## Arrange / Act / Assert Standard

Tests should use a clear AAA structure:

1. Arrange
2. Act
3. Assert

Recommended rules:
- keep arrange deterministic
- keep act to a single main behavior
- keep assert focused on one reason to fail

---

## Failure Diagnostics Standard

When a test fails, it should be obvious:
- which test failed
- what rule or behavior failed
- what the expected result was
- what the actual result was
- which case identifier the failure belongs to

Recommended practices:
- use explicit assertion messages where needed
- log seeded ids, dates, and users when integration setup matters
- avoid overloading one test with multiple unrelated concerns

---

## Shared Test Infrastructure Standard

Shared test helpers should be organized and reusable.

Recommended categories:
- `Builders/`
- `Fixtures/`
- `Common/`
- `TestConstants/`
- fixed-time helpers
- shared user helpers

Examples:
- `TestUserBuilder`
- `IncomeBuilder`
- `FixedDate`
- `PostgresFixture`
- `TestDatabaseReset`

Keep these reusable and independent of any one test file.

---

## Output Standard

Local test runs should provide:
- readable test names
- clear pass/fail status
- stack traces
- case identifiers
- suite and feature visibility

Recommended command pattern:

```bash
dotnet test <project> --logger "console;verbosity=detailed" --logger "trx;LogFileName=<suite>.trx"
```

Expected result:
- developers can identify the failure quickly
- CI can consume the TRX artifact later

---

## Summary Standard

At the end of a run, the summary should clearly show:
- total tests
- passed count
- failed count
- skipped count
- duration
- failed case identifiers
- feature grouping when available

Preferred reporting model:
- console summary for developers
- TRX for CI
- optional HTML summary later if CI reporting becomes richer

---

## Consistency Rule

Once a convention is chosen, apply it across:
- unit tests
- integration tests
- future CI workflows

Consistency matters more than personal style differences.
