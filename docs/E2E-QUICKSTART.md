# E2E Quickstart (Local + CI-Friendly)

This guide is a practical companion to `docs/TEST-PLAN.md`. It focuses on getting End-to-End (E2E) tests running locally in a repeatable way.

## What These E2E Tests Are
E2E here means: real HTTP requests against the API (GraphQL POST), real auth middleware, real DB calls, real serialization, and real status codes.

Recommended setup:
- In-process API via `WebApplicationFactory`
- Real Postgres database (Docker container)
- Test JWTs generated from `Auth:JwtSecret` (no Supabase dependency)

## Prereqs
- .NET SDK installed (repo targets `net10.0`)
- Docker available locally (for Postgres)

## Start a Local Postgres for Tests
Run Postgres in Docker (example uses port `5432`):

```bash
docker run --rm --name finance-tracker-test-db \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=finance_tracker_test \
  -p 5432:5432 \
  postgres:16
```

## Configure the API Under Test (E2E)
E2E tests should provide config via environment variables (not developer machine secrets).

Minimum required:
- `ConnectionStrings__Default`
- `Auth__JwtSecret`

Example (bash):

```bash
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=finance_tracker_test;Username=postgres;Password=postgres"
export Auth__JwtSecret="test-secret-min-32-chars-1234567890abcdef"
```

Optional:
- `Database__CommandTimeoutSeconds=60`
- `GraphQL__ExecutionTimeoutSeconds=60`

## Database Schema
Pick one approach and stick to it:

1. SQL-first (fast to bootstrap)
- Apply the scripts in `scripts/db/*.sql` in order (000..004).

2. EF migrations (if/when added to the repo)
- Run `dotnet ef database update` against the test DB.

If you want the tests to be fully self-contained, prefer approach (1) and have the E2E test fixture apply the SQL on startup.

## Running E2E Tests (Once the Test Project Exists)
The test plan proposes either:

Option A (separate project):
- `Tests/FinanceTracker.E2ETests/FinanceTracker.E2ETests.csproj`

Option B (combined):
- Put E2E tests into `FinanceTracker.ApiTests` and tag them (Trait/Category).

Example commands:

```bash
dotnet test Tests/FinanceTracker.E2ETests/FinanceTracker.E2ETests.csproj
```

Or if combined + tagged:

```bash
dotnet test Tests/FinanceTracker.ApiTests/FinanceTracker.ApiTests.csproj --filter "Category=E2E"
```

## Token Strategy (No Supabase Needed)
Because the API validates JWTs using `Auth:JwtSecret`, E2E tests can mint their own JWTs:
- Sign with the same `Auth__JwtSecret`
- Put the user id in `sub` claim (Guid)
- Keep expirations short (e.g., 5–15 minutes)

This allows reliable tests for:
- missing token (401)
- expired token (401)
- valid token (200)
- cross-user isolation (UserA token vs UserB token)

## Recommended First E2E Test Set
Keep the initial E2E suite lean and high signal:
- Auth enforcement (no token, expired token, valid token)
- Isolation (UserA data never visible/mutable by UserB)
- One CRUD “journey” per feature (income/expense/savings/loan)
- Dashboard totals from seeded data

## CI Notes
In CI, use a Postgres service container and set the same env vars:
- `ConnectionStrings__Default` points to the CI service
- `Auth__JwtSecret` fixed test value

If E2E runtime becomes too slow:
- Run unit + integration on every PR
- Run E2E on backend PRs only (or nightly)

