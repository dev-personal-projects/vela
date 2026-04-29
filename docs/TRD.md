# 🛠️ Technical Requirements Document (TRD)
## Finance Tracker — GraphQL + .NET + Kotlin

---

## 1. Tech Stack

| Layer | Technology |
|---|---|
| API | .NET 10, Hot Chocolate 15 (GraphQL) |
| Architecture | Clean Architecture |
| Database | Supabase (PostgreSQL) |
| Auth | Supabase Auth (credential verification) + own JWT tokens |
| ORM | Entity Framework Core |
| Mobile | Kotlin + Apollo GraphQL (Phase 2) |
| Runtime | .NET 10 |

---

## 2. Architecture Overview

```mermaid
flowchart TB
    subgraph Client
        CL[GraphQL Client]
    end

    subgraph API Layer
        GQL[GraphQL\nQuery / Mutation]
        AUTH[JWT Middleware\nAPI token validation]
    end

    subgraph Application Layer
        SVC[Services / Use Cases]
        RES[Result Pattern]
    end

    subgraph Domain Layer
        ENT[Entities]
        COM[Common / Result]
    end

    subgraph Infrastructure Layer
        REP[Repositories]
        EF[EF Core DbContext]
    end

    subgraph Supabase
        SUPA_AUTH[(Auth\nauth.users)]
        SUPA_DB[(PostgreSQL\nDatabase)]
    end

    CL -- Bearer JWT --> AUTH
    AUTH --> GQL
    GQL --> SVC
    SVC --> ENT
    SVC --> REP
    REP --> EF
    EF --> SUPA_DB
    AUTH -.signs in via auth service.-> SUPA_AUTH
    SVC --> RES
```

### Layer Responsibilities

| Layer | Responsibility |
|---|---|
| Domain | Core entities, enums, value objects, Result pattern |
| Application | Business logic, use cases, service interfaces |
| Infrastructure | EF Core DbContext, Supabase connection, repository implementations |
| API | GraphQL types, queries, mutations, JWT auth middleware — no business logic |

---

## 3. Project Structure

```mermaid
flowchart LR
    ROOT[FinanceTracker/]

    ROOT --> DOM[Domain/]
    ROOT --> APP[Application/]
    ROOT --> INF[Infrastructure/]
    ROOT --> API[Api/]
    ROOT --> PRG[Program.cs]

    DOM --> DOM_ENT[Entities/\nIncome, Expense\nSavingsGoal, Loan]
    DOM --> DOM_COM[Common/\nResult.cs]
    DOM --> DOM_ENM[Enums/\nLoanType.cs]

    APP --> APP_INT[Interfaces/\nIIncomeService\nIExpenseService\nISavingsService\nILoanService]
    APP --> APP_SVC[Services/\nIncomeService\nExpenseService\nSavingsService\nLoanService]
    APP --> APP_DTO[DTOs/\nInput & Output models]

    INF --> INF_DB[Data/\nAppDbContext.cs]
    INF --> INF_REP[Repositories/]
    INF --> INF_SUP[Supabase/\nSupabaseAuthProvider.cs]

    API --> API_GQL[GraphQL/\nQuery.cs\nMutation.cs]
    API --> API_TYP[Types/\nGraphQL type configs]
    API --> API_AUTH[Auth/\nJWT Middleware]
```

---

## 4. Domain Entities

```mermaid
classDiagram
    class Income {
        +Guid Id
        +Guid UserId
        +decimal Amount
        +string Source
        +string Description
        +DateTime Date
        +DateTime CreatedAt
    }

    class Expense {
        +Guid Id
        +Guid UserId
        +decimal Amount
        +string Category
        +string Description
        +DateTime Date
        +DateTime CreatedAt
    }

    class SavingsGoal {
        +Guid Id
        +Guid UserId
        +string Name
        +decimal TargetAmount
        +decimal CurrentAmount
        +decimal RemainingAmount()
        +DateTime Deadline
        +DateTime CreatedAt
        +decimal Progress()
    }

    class Loan {
        +Guid Id
        +Guid UserId
        +string CounterpartyName
        +string CounterpartyPhoneNumber
        +decimal Amount
        +decimal AmountRepaid
        +LoanType Type
        +string Description
        +DateTime Date
        +DateTime DueDate
        +DateTime CreatedAt
        +decimal RemainingBalance()
        +bool IsSettled()
    }

    class LoanType {
        <<enumeration>>
        Owed
        Given
    }

    Loan --> LoanType
```

> All entities carry `UserId` which maps to `auth.users.id` in Supabase.  
> Supabase Auth owns the user identity (email + password). We never create a users table.

---

## 5. GraphQL API Design

### Authentication Flow

```mermaid
sequenceDiagram
    participant Client
    participant SupabaseAuth
    participant API

    Client->>SupabaseAuth: POST /auth/v1/token (email + password)
    SupabaseAuth-->>Client: JWT access_token
    Client->>API: GraphQL request\nAuthorization: Bearer <JWT>
    API->>API: Validate JWT signature\n(Supabase JWT secret)
    API->>API: Extract UserId from claims
    API-->>Client: GraphQL response
```

### Queries

```mermaid
flowchart LR
    Q[Query Root\n🔒 Authenticated]

    Q --> QH[health: String\n🔓 public]
    Q --> QI[incomes: Income list\noptional fromDate / toDate / source]
    Q --> QE[expenses: Expense list\noptional fromDate / toDate / category]
    Q --> QS[savingsGoals: SavingsGoal list]
    Q --> QL[loans: Loan list\noptional type]
    Q --> QD[dashboard: DashboardSummary]
```

### Mutations

```mermaid
flowchart LR
    M[Mutation Root\n🔒 Authenticated]

    M --> MI[addIncome\nAddIncomeInput\n→ IncomePayload]
    M --> ME[addExpense\nAddExpenseInput\n→ ExpensePayload]
    M --> MS[createSavingsGoal\nCreateSavingsGoalInput\n→ SavingsGoalPayload]
    M --> MS2[addSavingsContribution\nAddSavingsContributionInput\n→ SavingsGoalPayload]
    M --> ML1[addLoan\nAddLoanInput\n→ LoanPayload]
    M --> ML2[recordRepayment\nRecordRepaymentInput\n→ LoanPayload]
```

---

## 6. Result Pattern

All service methods return `Result<T>` or `Result` — no exceptions thrown for business logic failures.

```mermaid
flowchart TD
    SVC[Service Method]
    SVC --> OK[Result.Ok / Result&lt;T&gt;.Ok]
    SVC --> FAIL[Result.Fail / error message]

    OK --> GQL_OK[GraphQL returns data]
    FAIL --> GQL_ERR[GraphQL returns error payload]
```

---

## 7. Data Flow — Add Expense Example

```mermaid
sequenceDiagram
    participant Client
    participant JWT Middleware
    participant GraphQL
    participant ExpenseService
    participant Repository
    participant Supabase DB

    Client->>JWT Middleware: mutation addExpense(input)\nBearer <JWT>
    JWT Middleware->>JWT Middleware: Validate token\nExtract UserId
    JWT Middleware->>GraphQL: forward request + UserId
    GraphQL->>ExpenseService: AddExpenseAsync(input, userId)
    ExpenseService->>ExpenseService: Validate input
    ExpenseService->>Repository: SaveAsync(expense)
    Repository->>Supabase DB: INSERT
    Supabase DB-->>Repository: OK
    Repository-->>ExpenseService: expense
    ExpenseService-->>GraphQL: Result&lt;Expense&gt;.Ok(expense)
    GraphQL-->>Client: ExpensePayload { expense }
```

---

## 8. Dashboard Query

```mermaid
flowchart TD
    DQ[dashboard Query]

    DQ --> TI[totalIncome\nSUM of all income]
    DQ --> TE[totalExpenses\nSUM of all expenses]
    DQ --> NB[netBalance\ntotalIncome - totalExpenses]
    DQ --> SP[savingsProgress\nlist of goals with current, remaining, and % progress]
    DQ --> LS[loanSummary\ntotal owed / total given]
```

---

## 9. Error Handling Strategy

| Scenario | Handling |
|---|---|
| Missing / invalid JWT | 401 Unauthorized before reaching GraphQL |
| Invalid input | Return `Result.Fail` with descriptive message |
| Entity not found | Return `Result.Fail("Not found")` |
| Unauthorized data access | UserId scoping prevents cross-user access |
| Unexpected exception | Caught at GraphQL layer, generic error returned |
| Validation errors | Handled in service layer before DB access |

---

## 10. Supabase Integration

```mermaid
flowchart LR
    subgraph Supabase
        SA[auth.users
managed by Supabase
email and password]
        DB[(PostgreSQL
incomes
expenses
savings_goals
loans)]
    end

    subgraph Backend
        JWT[JWT Middleware
validates token]
        EF[EF Core
Npgsql provider]
    end

    JWT -.JWT secret.-> SA
    EF --> DB
    DB -.user_id FK.-> SA
```

> We never create a users table. Supabase Auth manages `auth.users` entirely.  
> Our tables reference `auth.users.id` via `user_id` foreign keys.
> For this long-lived .NET backend, prefer a direct Postgres connection when possible, or the Supabase session pooler on port `5432`. Avoid the transaction pooler on port `6543` for normal EF Core / Npgsql traffic unless you have a specific reason to use transaction mode.

---

## 11. Development Phases

```mermaid
gantt
    title Finance Tracker — Build Plan
    dateFormat YYYY-MM-DD
    section Domain
        Entities & Enums         :a1, 2025-01-01, 3d
        Result Pattern            :done, a2, 2025-01-01, 1d
    section Infrastructure
        Supabase connection       :a3, after a1, 1d
        EF Core DbContext         :a4, after a3, 2d
        Repositories              :a5, after a4, 4d
    section Application
        Service Interfaces        :a6, after a1, 2d
        Service Implementations   :a7, after a6, 5d
    section API
        JWT Auth Middleware       :a8, after a3, 2d
        GraphQL Queries           :a9, after a7, 3d
        GraphQL Mutations         :a10, after a9, 3d
        Dashboard Query           :a11, after a10, 2d
    section Mobile
        Kotlin + Apollo           :a12, after a11, 14d
```
