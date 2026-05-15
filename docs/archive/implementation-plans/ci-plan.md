# CI Plan

## Purpose

This document defines the CI strategy for the Finance Tracker backend test workflow.

The main goal is simple:

**tests must pass before deployment can proceed.**

---

## Current Intent

For the current phase, CI should enforce:
- build success
- unit test success
- integration test success

E2E is intentionally deferred and should not block the first CI version.

---

## Workflow Structure

Recommended initial workflow files:

```text
.github/workflows/
  ci.yml
```

Later, this can expand to:

```text
.github/workflows/
  ci.yml
  deploy.yml
```

Where:
- `ci.yml` handles build and tests
- `deploy.yml` depends on successful CI

---

## CI Responsibilities

The first CI workflow should:
- restore dependencies
- build the solution or target project
- run unit tests
- start PostgreSQL for integration tests
- run integration tests
- publish test artifacts
- fail the workflow if any required step fails

---

## Deployment Gate Rule

Deployment should only proceed when:
- build passes
- unit tests pass
- integration tests pass

If any of those fail:
- deployment must not run

This is the default quality gate.

---

## Reporting Expectations

CI output should clearly show:
- build result
- unit test result
- integration test result
- failed test identifiers
- links or artifacts for detailed logs

Artifacts to keep:
- TRX files
- optional test summary output

---

## Recommended CI Sequence

1. Checkout code
2. Setup .NET SDK
3. Restore dependencies
4. Build project
5. Run unit tests
6. Start PostgreSQL service/container
7. Prepare test database
8. Run integration tests
9. Upload TRX artifacts
10. Mark workflow failed if any required step fails

---

## PostgreSQL Strategy In CI

Recommended approach:
- use a PostgreSQL service container inside GitHub Actions

Why:
- repeatable
- isolated
- easy to reason about
- good fit for integration tests

---

## Required Output Quality

The CI pipeline should feel easy to understand:
- suite names are obvious
- failed tests are traceable by case id
- developers can distinguish build failures from test failures
- integration failures are separated from unit failures

This should support a professional engineering workflow similar to large enterprise teams.

---

## Local-to-CI Alignment

Local commands and CI commands should stay as close as possible.

Recommended local equivalents:

```bash
dotnet test Tests/UnitTests/UnitTests.csproj --logger "console;verbosity=detailed" --logger "trx;LogFileName=unit-tests.trx"
dotnet test Tests/IntegrationTests/IntegrationTests.csproj --logger "console;verbosity=detailed" --logger "trx;LogFileName=integration-tests.trx"
```

This reduces “works locally but not in CI” problems.

---

## Future Expansion

Later additions may include:
- API/E2E workflow jobs
- coverage reporting
- HTML summaries
- branch protection rules
- deployment environments
- manual approval gates for production

These are follow-ups, not blockers for the first CI version.

---

## Exit Criteria

The CI plan is ready when:
- `ci.yml` is created
- unit tests run automatically
- integration tests run automatically
- PostgreSQL-backed integration setup works in CI
- TRX artifacts are published
- deployment is blocked when tests fail
