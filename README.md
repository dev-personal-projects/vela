# Vela API

Backend API for personal finance tracking (incomes, expenses, savings goals, loans) exposed as versioned REST endpoints on Azure Functions. Built in .NET with a Clean Architecture-style separation.

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
- Savings contributions and direct progress correction
- Loan repayment history, settlement, and status-aware filtering
- Optional `counterpartyPhoneNumber` on loans for contact-friendly clients
- Dashboard summary aggregates
- Public REST health/auth endpoints and JWT-protected finance operations

## Architecture
Layering:
- `Domain/`: entities and core types
- `Application/`: service-level business rules and interfaces
- `Infrastructure/`: EF Core/Postgres repositories and adapters
- `Api/`: Azure Functions HTTP triggers and API adapter helpers

## Getting started
Prereqs:
- .NET 10 SDK
- PostgreSQL (local Docker or Supabase Postgres)

Run:
```bash
func start --port 7071
```

Verify:
```bash
curl -s http://localhost:7071/api/v1/health
```

## Development environment
The supported team environment is the devcontainer.

Before opening the repository in the devcontainer for the first time:
1. Ensure Azure CLI is logged in with access to the development Key Vault.
2. Create `appsettings.Development.json` from `appsettings.Development.example.json`, or set `AzureKeyVault__VaultUri`.
3. Keep real secrets in Azure Key Vault, not in local JSON files.

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
- Azure Functions Core Tools and Azurite for local Function App execution
- a VS Code task that starts the API on `http://localhost:7071`

Recommended first-run flow inside the devcontainer:
1. Run `az login` if the devcontainer cannot see an active Azure CLI session.
2. Confirm your account can read `https://kv-vela-dev-neu.vault.azure.net/`.
3. Create `appsettings.Development.json` from `appsettings.Development.example.json` if needed.
4. Open the repository in the devcontainer.
5. Let the bootstrap finish.
6. Run `ensure dev db`, `unit tests`, or `integration tests` from the shared VS Code tasks when you want to recheck the environment manually.

## Configuration
Production secrets are loaded from Azure Key Vault during startup. Local JSON and environment variables are only for non-secret bootstrap configuration and operational settings.

Required Key Vault secrets:
- `Auth--JwtSecret`
- `ConnectionStrings--Default`
- `Supabase--Url`
- `Supabase--AnonKey`
- `Redis--ConnectionString`

Recommended for this persistent backend:
- Use a direct Postgres connection if available
- Otherwise use the Supabase session pooler on port `5432`
- Avoid the transaction pooler on port `6543` for normal EF Core / Npgsql backend traffic unless you specifically need transaction-mode pooling

Bootstrap environment variable examples:
- `AzureKeyVault__VaultUri`
- `AzureKeyVault__CredentialMode`
- `AzureKeyVault__ManagedIdentityClientId`

Development database bootstrapping:
- the devcontainer bootstrap runs `scripts/devcontainer/ensure-dev-db.sh`
- that script validates and applies the SQL files under `scripts/db` against the database configured in Azure Key Vault
- first-time contributors need Azure CLI access to the development Key Vault

## Endpoints
Default local Function host:
- Health: `GET /api/v1/health`
- Auth: `POST /api/v1/auth/signup`, `POST /api/v1/auth/signin`
- REST resources: `/api/v1/incomes`, `/api/v1/expenses`, `/api/v1/savings-goals`, `/api/v1/loans`, `/api/v1/dashboard`

Postman:
- `docs/postman/test-examples.json`

REST reference:
- `docs/rest-api.md`

GraphQL is preserved on the `graphql-learning` branch only.

## Documentation and lessons
Product/technical docs:
- REST API reference: `docs/rest-api.md`
- Azure Functions local development: `docs/functions-local-development.md`
- Azure Functions deployment runbook: `docs/functions-deployment-runbook.md`
- Commit conventions: `docs/git/commit-conventions.md`
- Commit message template: `.github/commit-message-template.md`
- Commit context tool: `scripts/git/prepare-commit-context.sh`
- Commit finalize tool: `scripts/git/finalize-commit.sh`
- Commit proposal file: `commit-message-proposal.md` (contains only the final commit message paragraph)

Commit workflow:
1. Stage the intended files manually.
2. Run `bash scripts/git/prepare-commit-context.sh`.
3. Review `commit-message-proposal.md`.
4. Tell the agent to proceed.
5. The agent runs `bash scripts/git/finalize-commit.sh`, commits staged changes only, pushes the current branch, and removes the generated proposal file.

Archived design history:
- GraphQL-era product, technical, lesson, and phase docs: `docs/archive/graphql-era/`
- Historical implementation plans: `docs/archive/implementation-plans/`
- GraphQL learning material is retained for the `graphql-learning` branch.

## Testing
Test plan and coverage:
- `docs/test-requirements.md`
- `docs/test-plan.md`
- `docs/unit-test-plan.md`
- `docs/integration-test-plan.md`
- E2E quickstart: `docs/e2e-quickstart.md`
