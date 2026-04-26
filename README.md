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
- Dashboard summary aggregates
- JWT-protected GraphQL operations

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

## Endpoints
Default (see console output / launch settings for the bound URL/port):
- Health: `GET /health`
- Auth: `POST /api/auth/signup`, `POST /api/auth/signin`
- GraphQL: `POST /graphql`

Postman:
- `docs/postman/test-examples.json`

## Documentation and lessons
Product/technical docs:
- PRD: `docs/PRD.md`
- TRD: `docs/TRD.md`

Lessons (guided learning path built around this codebase):
- Lesson index: `docs/lessons/README.md`
- Fundamentals: `docs/lessons/LESSON-1-graphql-fundamentals.md`
- Queries/mutations (Hot Chocolate): `docs/lessons/LESSON-2-queries-and-mutations.md`
- Error handling: `docs/lessons/LESSON-5-error-handling.md`
- Auth and security: `docs/lessons/LESSON-6-authentication-and-security.md`
- Architecture: `docs/lessons/LESSON-7-clean-architecture-and-graphql.md`

## Testing
Test plan and coverage:
- `docs/TEST-PLAN.md`
- E2E quickstart: `docs/E2E-QUICKSTART.md`
