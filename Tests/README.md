# Tests

This directory contains the automated test suites for the Vela backend.

The goal of this test structure is to keep the codebase:
- correct
- safe to change
- easy to diagnose when something breaks
- consistent across local development and CI

This document explains what lives in `Tests/`, how to run it, and how to extend it without drifting from the project testing architecture.

Related documentation:
- [Test Requirements](/workspaces/vela/docs/test-requirements.md)
- [Test Plan](/workspaces/vela/docs/test-plan.md)
- [Unit Test Plan](/workspaces/vela/docs/unit-test-plan.md)
- [Integration Test Plan](/workspaces/vela/docs/integration-test-plan.md)
- [Testing Conventions](/workspaces/vela/docs/testing-conventions.md)
- [Archived Devcontainer Plan](/workspaces/vela/docs/archive/implementation-plans/devcontainer-plan.md)

## Purpose

The test strategy for this repository is intentionally layered.

Each layer answers a different question:

| Layer | Location | Primary Question |
| --- | --- | --- |
| Unit tests | `Tests/UnitTests` | Does the business logic behave correctly in isolation? |
| Integration tests | `Tests/IntegrationTests` | Does the repository, database, and infrastructure behavior work correctly with real dependencies? |
| API integration tests | planned | Does the real HTTP/REST pipeline behave correctly end to end? |

The current repository implements the first two layers.

## Directory Structure

```text
Tests/
├── README.md
├── UnitTests/
│   ├── Auth/
│   ├── Dashboard/
│   ├── Expense/
│   ├── Income/
│   ├── Loan/
│   ├── Savings/
│   ├── Builders/
│   ├── Common/
│   └── UnitTests.csproj
└── IntegrationTests/
    ├── Caching/
    ├── Dashboard/
    ├── Expense/
    ├── Income/
    ├── Loan/
    ├── Savings/
    ├── Common/
    └── IntegrationTests.csproj
```

## Test Framework Standard

This repository standardizes on:

- `xUnit.net v3`
- `Microsoft.NET.Test.Sdk`
- `Moq` for mocking in unit tests
- `Respawn` for database reset in integration tests
- `Testcontainers.PostgreSql` for disposable PostgreSQL integration test environments

The command-line entry point for all suites is `dotnet test`.

For the clearest local and CI output, use the repository test runner:

```bash
scripts/test/run-tests.sh all
```

The runner streams detailed test progress while the suites execute, writes TRX result files under `artifacts/test-results/`, and prints final summary tables for the full run, each executed suite, slowest tests, and failures.

When integration tests run inside the supported devcontainer, the runner also prints a Testcontainers preflight block and applies `TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal` if the current shell is missing it.

## Unit Tests

Unit tests live under [Tests/UnitTests](/workspaces/vela/Tests/UnitTests).

These tests are intended to be:
- fast
- deterministic
- dependency-free
- safe to run frequently

They validate:
- service-layer business rules
- input validation
- orchestration behavior
- cache invalidation decisions
- failure-path handling

They should not require:
- PostgreSQL
- Docker
- Redis network access
- Supabase network calls
- a running application host

Current feature coverage includes:
- auth
- dashboard services
- incomes
- expenses
- loans
- savings goals

Run the full unit suite:

```bash
scripts/test/run-tests.sh unit
```

Run the full unit suite with TRX output:

```bash
scripts/test/run-tests.sh unit --configuration Release --no-restore
```

## Integration Tests

Integration tests live under [Tests/IntegrationTests](/workspaces/vela/Tests/IntegrationTests).

These tests validate infrastructure behavior with real dependencies, especially:
- EF Core mappings
- PostgreSQL behavior
- repository filtering and ordering
- user scoping
- destructive operations such as delete
- dashboard aggregation
- cache integration behavior

### Current Integration Areas

The current implemented integration suites cover:

- dashboard repository behavior
- dashboard cache service behavior
- income repository behavior
- expense repository behavior
- savings goal repository behavior
- loan repository behavior
- loan repayment repository behavior

Shared infrastructure for these tests lives under [Tests/IntegrationTests/Common](/workspaces/vela/Tests/IntegrationTests/Common).

### Database Model

Postgres-backed integration tests use:
- `Testcontainers PostgreSQL`
- a disposable database container for the test run
- `Respawn` to reset database state between scenarios

These tests must not point at:
- a normal development database
- a staging database
- a production database
- any database with real user data

The repository standard is strict here:
- automated database-backed integration tests use Testcontainers
- there is no fallback database path for those tests

### Running Integration Tests

Run the integration test project:

```bash
scripts/test/run-tests.sh integration
```

Run with TRX output:

```bash
scripts/test/run-tests.sh integration --configuration Release --no-restore
```

### Environment Expectations

For Postgres-backed integration tests to execute fully, the environment must provide:
- Docker
- Testcontainers support
- the supported devcontainer setup

If Docker/Testcontainers access is unavailable, the Postgres-backed integration tests are expected to skip rather than run against a substitute database.

That behavior is controlled by:
- [DockerAvailability.cs](/workspaces/vela/Tests/IntegrationTests/Common/DockerAvailability.cs)
- [IntegrationFactAttribute.cs](/workspaces/vela/Tests/IntegrationTests/Common/IntegrationFactAttribute.cs)

## Recommended Local Workflow

For normal development, use this sequence:

1. Run unit tests after logic changes.
2. Run integration tests after repository or infrastructure changes.
3. Run both before opening or updating a pull request.

Recommended commands:

```bash
scripts/test/run-tests.sh unit --no-restore
scripts/test/run-tests.sh integration --no-restore
```

If you are using the supported devcontainer, equivalent VS Code tasks are available for:
- `unit tests`
- `integration tests`

## Running Tests in the Devcontainer

The supported developer environment for this repository is the devcontainer.

Before opening the repository in the devcontainer for the first time:
- ensure `appsettings.Development.json` exists
- ensure it contains valid development credentials and connection strings

Once that is in place, the devcontainer is expected to handle:
- dependency restore
- environment validation
- development database schema validation and migration
- Docker/Testcontainers readiness checks

Relevant scripts:
- [bootstrap.sh](/workspaces/vela/scripts/devcontainer/bootstrap.sh)
- [validate-env.sh](/workspaces/vela/scripts/devcontainer/validate-env.sh)
- [check-testcontainers.sh](/workspaces/vela/scripts/devcontainer/check-testcontainers.sh)
- [ensure-dev-db.sh](/workspaces/vela/scripts/devcontainer/ensure-dev-db.sh)

## Conventions for New Tests

New tests should follow the shared conventions in [Testing Conventions](/workspaces/vela/docs/testing-conventions.md).

At a minimum:
- use readable Arrange / Act / Assert structure
- keep one main reason to fail per test
- use stable case identifiers
- keep feature ownership obvious from file placement

Recommended case ID formats:

```text
UT-<FEATURE>-<AREA>-<NUMBER>
IT-<FEATURE>-<AREA>-<NUMBER>
```

Examples:

```text
UT-AUTH-SIGNIN-001
IT-LOAN-REPAY-REPO-001
```

## How to Decide Where a Test Belongs

Use this rule of thumb:

- if the behavior is pure business logic or orchestration, add a unit test
- if the behavior depends on PostgreSQL, EF Core mapping, real ordering, filtering, or reset behavior, add an integration test
- if the behavior depends on HTTP, authorization middleware, response payload shape, or end-to-end request handling, add an API integration test

Avoid forcing infrastructure behavior into unit tests with unrealistic mocks.

## Current Gaps

The test architecture is not complete yet.

Planned next additions:
- API-level REST integration tests
- REST endpoint tests for auth and health
- more consistent trait usage across suites
- optional stronger cache-environment coverage if Redis-specific scenarios become necessary

That means this `Tests` directory is already substantial, but it does not yet represent the full planned backend test matrix.

## Troubleshooting

### Unit Tests Fail

Start with:
- service logic changes
- mock setup drift
- assertion expectations
- shared test helper regressions

### Integration Tests Are Skipped

Most commonly this means Docker/Testcontainers is not available in the current environment.

Check:
- Docker is available
- the repository is opened in the supported devcontainer
- Docker socket passthrough is working

### Integration Tests Fail Before Assertions

Check:
- Testcontainers startup
- `TESTCONTAINERS_HOST_OVERRIDE` is set to `host.docker.internal` inside the devcontainer
- database reset behavior
- EF mapping changes
- schema assumptions
- connection issues in the current environment

### Development Database Issues

If the dev environment appears out of sync, rerun:

```bash
bash scripts/devcontainer/ensure-dev-db.sh
```

This validates and reapplies the SQL under [scripts/db](/workspaces/vela/scripts/db) against the configured development database.

## Review Standard

Changes to the test suite should be reviewed with the same care as production code.

Good test changes are:
- readable
- intentional
- stable
- scoped to behavior
- diagnostic when they fail

Weak test changes usually show up as:
- vague names
- mixed responsibilities in one test
- hidden environment assumptions
- over-mocked infrastructure behavior
- assertions that do not explain the failure clearly

## Summary

Use this directory as the operational entry point for backend testing:
- `Tests/UnitTests` for isolated business logic
- `Tests/IntegrationTests` for real infrastructure behavior
- `dotnet test` as the standard runner
- the devcontainer as the standard local environment

If a contributor can open this folder, read this document, and know how to run and extend the suites safely, then this README is doing its job.
