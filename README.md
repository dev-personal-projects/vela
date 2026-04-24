# Finance Tracker API (GraphQL + .NET)

Backend service for personal finance tracking: incomes, expenses, savings goals, loans, and a dashboard summary. Built with .NET + Hot Chocolate GraphQL using a Clean Architecture-style layering.

## Features
- Income: add/update/list/get/delete
- Expense: add/update/list/get/delete
- Savings goals: create/update/progress/list/get/delete
- Loans: add/update/repay/list/get/delete
- Dashboard: aggregated totals and summaries

## Tech Stack
- .NET 10
- Hot Chocolate GraphQL
- EF Core + PostgreSQL (Supabase-compatible)
- JWT auth (Supabase used for credential verification; API issues its own access token)

## Repo Layout
```
Api/            HTTP + GraphQL (queries/mutations, controllers, auth wiring)
Application/    Services (business rules) + DTOs + interfaces
Domain/         Entities + enums + result types
Infrastructure/ EF Core DbContext + repository implementations + auth adapters
docs/           PRD/TRD, test plan, Postman examples
scripts/        SQL setup scripts and utilities
```

## Run Locally
Prereqs: .NET 10 SDK and a PostgreSQL database (local or Supabase).

1. Configure settings (via `appsettings.json` or env vars):
   - `Auth:JwtSecret` (>= 32 chars)
   - `ConnectionStrings:Default` (Postgres connection string or URI)
   - `Supabase:Url`, `Supabase:AnonKey` (required for `/api/auth/*` sign-in/sign-up)

2. Run:
```bash
dotnet run
```

Default endpoints:
- Health: `GET /health`
- Auth: `POST /api/auth/signup`, `POST /api/auth/signin`
- GraphQL: `POST /graphql`

## Postman Examples
Use `docs/postman/test-examples.json` for a guided request sequence, including JWT usage and GraphQL queries/mutations.

## Testing
- Test strategy and coverage matrix: `docs/TEST-PLAN.md`
- E2E quickstart (DB + env vars + JWT strategy): `docs/E2E-QUICKSTART.md`
