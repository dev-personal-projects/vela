# Project Phases — Overview

## Finance Tracker Backend — Build Plan

---

## Phase Summary

| Phase | Name | Focus | Status |
|---|---|---|---|
| [Phase 0](./PHASE-0-foundation.md) | Foundation | Project setup, Supabase, JWT, EF Core | ✅ Complete |
| [Phase 1](./PHASE-1-domain.md) | Domain | Entities, enums, business rules | ✅ Complete |
| [Phase 2](./PHASE-2-infrastructure.md) | Infrastructure | DbContext, SQL migrations, repositories, dashboard read model, caching infra | ✅ Complete |
| [Phase 3](./PHASE-3-application.md) | Application | Services, DTOs, validation, dashboard orchestration, cache invalidation | ✅ Complete |
| [Phase 4](./PHASE-4-graphql-api.md) | GraphQL API | Queries, mutations, dashboard, auth-protected resolver layer | ✅ Complete |
| [Phase 5](./PHASE-5-testing.md) | Testing | Unit, integration, auth isolation | 🚧 Planned / Partial |

---

## Build Order

```mermaid
flowchart LR
    P0[Phase 0\nFoundation]
    P1[Phase 1\nDomain]
    P2[Phase 2\nInfrastructure]
    P3[Phase 3\nApplication]
    P4[Phase 4\nGraphQL API]
    P5[Phase 5\nTesting]

    P0 --> P1
    P1 --> P2
    P1 --> P3
    P2 --> P3
    P3 --> P4
    P4 --> P5
```

---

## Feature Coverage Per Phase

```mermaid
flowchart TB
    subgraph Features
        F1[Income]
        F2[Expenses]
        F3[Savings Goals]
        F4[Loans]
        F5[Dashboard]
    end

    subgraph Phase 1 - Domain
        D1[Income entity]
        D2[Expense entity]
        D3[SavingsGoal entity]
        D4[Loan entity]
        D5[LoanType enum]
    end

    subgraph Phase 2 - Infrastructure
        R1[IncomeRepository]
        R2[ExpenseRepository]
        R3[SavingsGoalRepository]
        R4[LoanRepository]
        R5[DashboardRepository]
        R6[DashboardCacheService]
    end

    subgraph Phase 3 - Application
        S1[IncomeService]
        S2[ExpenseService]
        S3[SavingsGoalService]
        S4[LoanService]
        S5[DashboardService]
    end

    subgraph Phase 4 - API
        A1[income queries and mutations]
        A2[expense queries and mutations]
        A3[savings queries and mutations]
        A4[loan queries and mutations]
        A5[dashboard query]
    end

    F1 --> D1 --> R1 --> S1 --> A1
    F2 --> D2 --> R2 --> S2 --> A2
    F3 --> D3 --> R3 --> S3 --> A3
    F4 --> D4 --> R4 --> S4 --> A4
    F5 --> R5 --> S5 --> A5
```

---

## Layer Dependency Rules

```mermaid
flowchart TB
    subgraph API
        GQL[GraphQL\nQuery and Mutation]
    end

    subgraph Application
        SVC[Services]
        ISVC[IService interfaces]
        IREPO[IRepository interfaces]
        DTO[DTOs]
    end

    subgraph Domain
        ENT[Entities]
        ENM[Enums]
        RES[Result]
    end

    subgraph Infrastructure
        REPO[Repositories]
        CTX[AppDbContext]
    end

    subgraph Supabase
        AUTH[(Auth\nauth.users)]
        DB[(PostgreSQL)]
    end

    GQL --> ISVC
    GQL --> DTO
    SVC --> IREPO
    SVC --> ENT
    SVC --> RES
    ISVC -.implemented by.-> SVC
    IREPO -.implemented by.-> REPO
    REPO --> CTX
    CTX --> DB
    GQL -.JWT validation.-> AUTH
```

---

## Build Timeline

```mermaid
gantt
    title Finance Tracker - Backend Build Plan
    dateFormat YYYY-MM-DD

    section Phase 0 Foundation
        Project structure       :done,  p0a, 2025-01-01, 1d
        Result pattern          :done,  p0b, 2025-01-01, 1d
        Supabase and EF Core    :       p0c, 2025-01-02, 2d
        JWT middleware          :       p0d, after p0c, 1d

    section Phase 1 Domain
        Enums                   :       p1a, after p0d, 1d
        Income entity           :       p1b, after p1a, 1d
        Expense entity          :       p1c, after p1a, 1d
        SavingsGoal entity      :       p1d, after p1a, 1d
        Loan entity             :       p1e, after p1a, 1d

    section Phase 2 Infrastructure
        AppDbContext            :       p2a, after p1e, 1d
        SQL migrations          :       p2b, after p2a, 1d
        Repositories            :       p2c, after p2b, 3d

    section Phase 3 Application
        DTOs                    :       p3a, after p1e, 1d
        Service interfaces      :       p3b, after p3a, 1d
        Service implementations :       p3c, after p2c, 4d

    section Phase 4 GraphQL API
        Income and Expense      :       p4a, after p3c, 2d
        Savings and Loans       :       p4b, after p4a, 2d
        Dashboard query         :       p4c, after p4b, 1d

    section Phase 5 Testing
        Unit tests              :       p5a, after p4c, 3d
        Integration tests       :       p5b, after p5a, 3d
        Auth and isolation      :       p5c, after p5b, 2d
```

---

## Key Rules Across All Phases

- Domain has zero external dependencies
- Application depends only on Domain
- Infrastructure implements Application interfaces — depends on Domain and Application
- API depends on Application interfaces only — never on Infrastructure directly
- Every service method returns `Result<T>` or `Result`
- Every resolver extracts `UserId` from the JWT `ClaimsPrincipal`
- Every repository query is scoped by `UserId`
- No business logic in resolvers — services own all logic
- No EF Core references in Domain or Application layers
- Dashboard caching lives behind an application interface and infrastructure implementation
