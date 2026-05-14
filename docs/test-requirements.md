# Test Requirements

## Purpose

This document defines the required test layers, test environments, and database safety rules for the Finance Tracker backend.

The goal is to keep development and testing reliable while protecting real user data. Tests that need a database must use an isolated test database, never the main development, staging, or production database.

Related docs:
- [test-plan.md](/workspaces/finance-tracker-graphql/docs/test-plan.md)
- [unit-test-plan.md](/workspaces/finance-tracker-graphql/docs/unit-test-plan.md)
- [integration-test-plan.md](/workspaces/finance-tracker-graphql/docs/integration-test-plan.md)
- [testing-conventions.md](/workspaces/finance-tracker-graphql/docs/testing-conventions.md)
- [ci-plan.md](/workspaces/finance-tracker-graphql/docs/ci-plan.md)

## Test Layers

The project should keep four clear test layers.

| Layer | Project or Location | Main Purpose | External Dependencies |
| --- | --- | --- | --- |
| Unit | `Tests/UnitTests` | Business rules, service orchestration, validation, cache invalidation decisions | None |
| Infrastructure integration | `Tests/IntegrationTests` | EF Core mappings, repositories, SQL behavior, database user scoping | Isolated PostgreSQL test database |
| Cache integration | `Tests/IntegrationTests/Caching` | Distributed cache keying, serialization, invalidation behavior | Memory cache by default; Redis in optional environment |
| API integration | planned `Tests/ApiTests` or `Tests/IntegrationTests/Api` | GraphQL/HTTP pipeline, auth rejection, resolver payloads, error formatting | Test host plus isolated PostgreSQL test database |

E2E or client-driven tests can be added later, but they should not replace the API integration layer.

## Environment Requirements

### Development Environment

Development runs against a developer-owned database or Supabase project.

Required:
- `Auth:JwtSecret`
- `ConnectionStrings:Default`
- `Supabase:Url`
- `Supabase:AnonKey`

Rules:
- development credentials must stay local
- real-looking values must not be committed
- development data may be manually inspected and changed
- automated test resets must never point at this database

Recommended local files:
- `appsettings.Development.json` for non-secret Key Vault bootstrap values only
- Azure Key Vault for anything sensitive

Recommended future test-only files:
- `appsettings.Test.json` for non-secret test defaults
- a test-only Azure Key Vault for test database credentials
- no committed real passwords, tokens, Redis secrets, or Supabase keys

### Unit Test Environment

Unit tests must be dependency-free.

Rules:
- no PostgreSQL
- no Supabase network calls
- no Redis network calls
- no ASP.NET test server
- all collaborators mocked or replaced with deterministic fakes

Expected command:

```bash
dotnet test Tests/UnitTests/UnitTests.csproj --logger "console;verbosity=detailed"
```

### Integration Test Environment

Integration tests must use a real PostgreSQL database that is separate from all developer and production databases.

Required setup:
- Testcontainers PostgreSQL
- disposable database per test run
- Respawn reset before each repository test

The only supported database path for automated integration tests is Testcontainers PostgreSQL. Testcontainers is the standard test database environment for local and CI runs.

Development is expected to run inside devcontainers. The devcontainer must provide Docker/Testcontainers access so integration tests can create the required disposable PostgreSQL database.

Forbidden:
- production database
- staging database used by real testers
- normal development database
- any database where destructive reset would be unsafe

Expected command:

```bash
dotnet test Tests/IntegrationTests/IntegrationTests.csproj --logger "console;verbosity=detailed"
```

Environment failure behavior:
- Postgres-backed integration tests require Docker/Testcontainers access from the devcontainer and CI.
- If Docker/Testcontainers is unavailable, fix the devcontainer or CI setup.
- Do not point the tests at another database to work around missing Docker/Testcontainers support.

### Real Database Connection Policy

Some integration tests must use a real database connection because EF Core mapping, PostgreSQL enum behavior, SQL ordering, deletes, and dashboard aggregation cannot be trusted from mocks alone.

The real database connection must point to:
- a Testcontainers PostgreSQL instance created only for the test run

The test database model is:
- disposable container created on demand by the test run
- no permanent shared test database service inside the devcontainer
- no alternate SQL source of truth under `.devcontainer/`
- project SQL assets remain under [scripts](/workspaces/finance-tracker-graphql/scripts)
- test/bootstrap code may read SQL files from `scripts/` when needed

The real database connection must not point to:
- the main app database
- a shared development database
- a dedicated developer database
- a database with manually curated data
- a separate Supabase project
- a CI-provisioned long-lived PostgreSQL service
- any database that cannot be safely reset

### API Integration Test Environment

API integration tests should exercise the real ASP.NET Core pipeline through an in-memory test host.

Required coverage:
- public GraphQL `health` query succeeds without JWT
- protected GraphQL queries reject missing or invalid JWTs
- protected GraphQL queries succeed with a valid API JWT
- successful mutations return expected payload shapes
- validation failures return expected GraphQL error or payload format
- user A cannot read or modify user B data through GraphQL
- REST auth endpoints return appropriate status codes and response bodies
- GraphQL error formatter does not leak exception details

Recommended setup:
- `WebApplicationFactory` or equivalent ASP.NET Core test host
- isolated PostgreSQL test database
- deterministic JWT factory
- fake or stub Supabase auth client where possible
- one seeded user per scenario unless a cross-user isolation test needs two users

API tests should be slower than unit tests but still deterministic and safe to run in CI.

## Database Safety Requirements

Any automated test that can create, update, delete, truncate, or reset rows must pass these checks.

| Requirement | Rule |
| --- | --- |
| Isolation | Test database must be created by Testcontainers for the current test run. |
| Reset safety | Reset logic may run only against the Testcontainers database. |
| Least surprise | Test setup must make destructive behavior obvious. |
| Secrets | No real database credentials are required for Postgres-backed integration tests. |
| Determinism | Tests seed their own data and do not depend on existing rows. |
| User scoping | Tests include cross-user cases for all user-owned resources. |

The integration fixture should fail closed:
- if Testcontainers cannot create the database, treat it as an environment setup failure
- reset logic must not accept arbitrary connection strings

## Required Coverage Matrix

| Feature | Unit Tests | DB Integration Tests | API Integration Tests |
| --- | --- | --- | --- |
| Auth | sign-up/sign-in validation, token orchestration | optional provider adapter contract tests | REST auth success/failure, GraphQL JWT acceptance/rejection |
| Income | add/update/delete validation, cache invalidation | persistence, ordering, user scoping, delete safety | query filtering, add/update/delete payloads, auth rejection |
| Expense | add/update/delete validation, cache invalidation | persistence, ordering, user scoping, delete safety | query filtering, add/update/delete payloads, auth rejection |
| Savings | create/update/contribution/progress rules | persistence, progress values, user scoping | create/update/contribution/progress payloads |
| Loans | add/update/repay/settle/delete rules | enum mapping, repayment persistence, user scoping | loan filters, repayment history, settlement payloads |
| Dashboard | summary composition, cache orchestration | aggregate SQL, recent activity merge, user scoping | dashboard query filters, auth rejection, error behavior |
| Error handling | service result failures | database constraint regressions where needed | GraphQL error shape, HTTP status mapping |
| Caching | cache hit/miss orchestration | cache service behavior | dashboard API behavior after mutation invalidation |

## Done Criteria

A feature is considered well tested when:
- important business rules have unit tests
- persistence behavior has integration tests when database mapping or SQL is involved
- user scoping is tested at repository and API levels
- successful and failing API paths are covered for user-facing operations
- tests have stable case IDs and readable display names
- tests can run locally without touching real user data
- CI can run the same suites with clear pass/fail output

## Current Gaps

Known gaps to address next:
- add API-level GraphQL integration tests
- add REST controller tests for auth and health
- make xUnit traits consistent across all unit and integration tests
- decide whether cache integration should include an optional Redis-backed suite
- make CI enforce that Postgres-backed integration tests are not skipped
