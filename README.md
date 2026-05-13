# Finance Tracker API

Backend API for personal finance tracking (incomes, expenses, savings goals, loans) with a GraphQL surface and a small REST auth/health layer. Built in .NET with a Clean Architecture-style separation.

## Contents
- Overview
- Architecture
- Getting started
- Configuration
- Endpoints
- Documentation and lessons
- Testing

## Overview
Core capabilities:
- Income, expense, savings goal, and loan CRUD (user-scoped)
- Savings contributions via `addSavingsContribution`, plus direct progress correction via `updateSavingsProgress`
- Loan repayment history, settlement, and status-aware filtering
- Optional `counterpartyPhoneNumber` on loans for contact-friendly clients
- Dashboard summary aggregates
- Public GraphQL `health` query and JWT-protected finance operations

## Architecture
Layering:
- `Domain/`: entities and core types
- `Application/`: service-level business rules and interfaces
- `Infrastructure/`: EF Core/Postgres repositories and adapters
- `Api/`: HTTP controllers + GraphQL queries/mutations and middleware

## Getting started
Prereqs:
- .NET 10 SDK
- PostgreSQL (local Docker or Supabase Postgres)

Run:
```bash
dotnet run
```

Verify:
```bash
curl -s http://localhost:8080/health
```

## Development environment
The supported team environment is the devcontainer.

Before opening the repository in the devcontainer for the first time:
1. Create or obtain `appsettings.Development.json`.
2. Ensure it contains valid development credentials and connection strings for this project.
3. Keep that file in the repository root before the devcontainer bootstrap runs.

Key files:
- `.devcontainer/devcontainer.json`
- `.devcontainer/Dockerfile.Dev`
- `.devcontainer/postCreate.sh`
- `.devcontainer/postStart.sh`
- `scripts/devcontainer/bootstrap.sh`
- `scripts/devcontainer/validate-env.sh`
- `scripts/devcontainer/check-testcontainers.sh`
- `scripts/devcontainer/ensure-dev-db.sh`

What the devcontainer now provides:
- Docker socket passthrough for Testcontainers-based integration tests
- shared CLI tooling such as `gh`, `jq`, `rg`, `psql`, `docker`, `redis-cli`, `tree`, and `make`
- startup bootstrap and environment validation
- automatic schema validation and migration for the configured development database using `scripts/db`
- shared VS Code tasks for restore, build, run, unit tests, integration tests, formatting, and commit workflow steps
- a debug profile that exposes the API on `http://0.0.0.0:8080`

Recommended first-run flow inside the devcontainer:
1. Prepare `appsettings.Development.json` with valid development credentials before opening the repo in the devcontainer.
2. Open the repository in the devcontainer.
3. Let the bootstrap finish.
4. Run `ensure dev db`, `unit tests`, or `integration tests` from the shared VS Code tasks when you want to recheck the environment manually.

## Configuration
Config can be set via `appsettings.json`, `appsettings.Development.json`, or environment variables.

Required:
- `Auth:JwtSecret` (>= 32 chars)
- `ConnectionStrings:Default` (Postgres connection string or URI)

Recommended for this persistent backend:
- Use a direct Postgres connection if available
- Otherwise use the Supabase session pooler on port `5432`
- Avoid the transaction pooler on port `6543` for normal EF Core / Npgsql backend traffic unless you specifically need transaction-mode pooling

For `/api/auth/*` sign-up/sign-in:
- `Supabase:Url`
- `Supabase:AnonKey`

Environment variable equivalents (examples):
- `Auth__JwtSecret`
- `ConnectionStrings__Default`
- `Supabase__Url`
- `Supabase__AnonKey`

Development database bootstrapping:
- the devcontainer bootstrap runs `scripts/devcontainer/ensure-dev-db.sh`
- that script validates and applies the SQL files under `scripts/db` against the database configured in `appsettings.Development.json`
- first-time contributors must have `appsettings.Development.json` ready before opening the repository in the devcontainer

## Endpoints
Default (see console output / launch settings for the bound URL/port):
- Health: `GET /health`
- Auth: `POST /api/auth/signup`, `POST /api/auth/signin`
- GraphQL: `POST /graphql`

Postman:
- `docs/postman/test-examples.json`

GraphQL highlights:
- Public query: `health`
- Filtered queries: `incomes(fromDate?, toDate?, source?)`, `expenses(fromDate?, toDate?, category?)`, `loans(type?, status?)`
- Savings: `addSavingsContribution`, `updateSavingsProgress`
- Loans: `recordRepayment`, `loanRepaymentHistory`, `settleLoan`

## Documentation and lessons
Product/technical docs:
- PRD: `docs/prd.md`
- TRD: `docs/trd.md`
- Devcontainer plan: `docs/devcontainer-plan.md`
- Commit conventions: `docs/git/commit-conventions.md`
- Commit message template: `docs/git/commit-message-template.md`
- Commit context tool: `scripts/git/prepare-commit-context.sh`
- Commit finalize tool: `scripts/git/finalize-commit.sh`
- Commit proposal file: `commit-message-proposal.md` (contains only the final commit message paragraph)

Commit workflow:
1. Stage the intended files manually.
2. Run `bash scripts/git/prepare-commit-context.sh`.
3. Review `commit-message-proposal.md`.
4. Tell the agent to proceed.
5. The agent runs `bash scripts/git/finalize-commit.sh`, commits staged changes only, pushes the current branch, and removes the generated proposal file.

Lessons (guided learning path built around this codebase):
- Lesson index: `docs/lessons/README.md`
- Fundamentals: `docs/lessons/lesson-01-graphql-fundamentals.md`
- Queries/mutations (Hot Chocolate): `docs/lessons/lesson-02-queries-and-mutations.md`
- Error handling: `docs/lessons/lesson-05-error-handling.md`
- Auth and security: `docs/lessons/lesson-06-authentication-and-security.md`
- Architecture: `docs/lessons/lesson-07-clean-architecture-and-graphql.md`

## Testing
Test plan and coverage:
- `docs/test-requirements.md`
- `docs/test-plan.md`
- `docs/unit-test-plan.md`
- `docs/integration-test-plan.md`
- E2E quickstart: `docs/e2e-quickstart.md`
