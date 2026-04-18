# Phase 4 — GraphQL API Layer

## Goal
Expose all features through a clean, authenticated GraphQL API. Resolvers are thin — extract `UserId` from the JWT, call the service, return the result. No business logic lives here.

---

## Checklist

- [ ] JWT Bearer + Hot Chocolate authorization wired in `Program.cs`
- [ ] `signUp` mutation calls Supabase Auth and returns JWT
- [ ] `signIn` mutation calls Supabase Auth and returns JWT
- [ ] All queries protected with `[Authorize]`
- [ ] All mutations except `signUp` and `signIn` protected with `[Authorize]`
- [ ] `UserId` extracted from `ClaimsPrincipal` in every protected resolver
- [ ] `health` query remains public
- [ ] All mutations return typed payloads with data or error
- [ ] Dashboard query returns aggregated summary

---

## Folder Structure

```
Api/
└── GraphQL/
    ├── Query.cs
    └── Mutation.cs

Application/
├── Interfaces/
│   └── IAuthService.cs
├── Services/
│   └── AuthService.cs
└── DTOs/
    └── Auth/
        ├── SignUpInput.cs
        ├── SignInInput.cs
        └── AuthPayload.cs
```

---

## Full API Map

```mermaid
flowchart TB
    subgraph Queries
        QH[health\npublic]
        QI[incomes\nauthenticated]
        QE[expenses\nauthenticated]
        QS[savingsGoals\nauthenticated]
        QL[loans\nauthenticated]
        QD[dashboard\nauthenticated]
    end

    subgraph Mutations
        MSU[signUp\npublic]
        MSI[signIn\npublic]
        MI[addIncome\nauthenticated]
        ME[addExpense\nauthenticated]
        MS[createSavingsGoal\nauthenticated]
        ML[addLoan\nauthenticated]
        MR[recordRepayment\nauthenticated]
    end
```

---

## Resolver Pattern

```mermaid
flowchart TD
    REQ[Incoming GraphQL request]
    JWT[JWT Middleware\nvalidate token\nextract UserId]
    RES[Resolver method]
    SVC[Service call\nservice.MethodAsync - input and userId]
    CHK{Result.IsSuccess}
    PAY[Return payload with entity]
    ERR[Return payload with error message]

    REQ --> JWT
    JWT --> RES
    RES --> SVC
    SVC --> CHK
    CHK -- yes --> PAY
    CHK -- no --> ERR
```

---

## Feature — Authentication

```mermaid
flowchart LR
    subgraph Mutations - public
        MSU[signUp\nSignUpInput\nreturns AuthPayload]
        MSI[signIn\nSignInInput\nreturns AuthPayload]
    end

    MSU --> AS[AuthService.SignUpAsync\ncalls Supabase Auth API]
    MSI --> AS2[AuthService.SignInAsync\ncalls Supabase Auth API]
```

---

## Sign Up Flow

```mermaid
sequenceDiagram
    participant Client
    participant Mutation
    participant AuthService
    participant SupabaseAuth

    Client->>Mutation: signUp(email, password)
    Mutation->>AuthService: SignUpAsync(email, password)
    AuthService->>SupabaseAuth: POST /auth/v1/signup
    SupabaseAuth->>SupabaseAuth: hash password
    SupabaseAuth->>SupabaseAuth: create auth.users row
    SupabaseAuth-->>AuthService: JWT access_token
    AuthService-->>Mutation: Result.Ok(token)
    Mutation-->>Client: AuthPayload with token
```

---

## Sign In Flow

```mermaid
sequenceDiagram
    participant Client
    participant Mutation
    participant AuthService
    participant SupabaseAuth

    Client->>Mutation: signIn(email, password)
    Mutation->>AuthService: SignInAsync(email, password)
    AuthService->>SupabaseAuth: POST /auth/v1/token
    SupabaseAuth->>SupabaseAuth: verify password
    SupabaseAuth-->>AuthService: JWT access_token
    AuthService-->>Mutation: Result.Ok(token)
    Mutation-->>Client: AuthPayload with token
```

---

## Feature — Income

```mermaid
flowchart LR
    subgraph Query
        QI[incomes\nreturns list of Income]
    end

    subgraph Mutation
        MI[addIncome\nAddIncomeInput\nreturns IncomePayload]
    end

    QI --> IS[IncomeService.GetAllAsync]
    MI --> IS2[IncomeService.AddAsync]
```

---

## Feature — Expenses

```mermaid
flowchart LR
    subgraph Query
        QE[expenses\nfilter by date and category\nreturns list of Expense]
    end

    subgraph Mutation
        ME[addExpense\nAddExpenseInput\nreturns ExpensePayload]
    end

    QE --> ES[ExpenseService.GetAllAsync]
    ME --> ES2[ExpenseService.AddAsync]
```

---

## Feature — Savings Goals

```mermaid
flowchart LR
    subgraph Query
        QS[savingsGoals\nreturns list of SavingsGoal]
    end

    subgraph Mutation
        MS[createSavingsGoal\nCreateSavingsGoalInput\nreturns SavingsGoalPayload]
    end

    QS --> SS[SavingsGoalService.GetAllAsync]
    MS --> SS2[SavingsGoalService.CreateAsync]
```

---

## Feature — Loans

```mermaid
flowchart LR
    subgraph Query
        QL[loans\nfilter by type\nreturns list of Loan]
    end

    subgraph Mutations
        ML[addLoan\nAddLoanInput\nreturns LoanPayload]
        MR[recordRepayment\nRecordRepaymentInput\nreturns LoanPayload]
    end

    QL --> LS[LoanService.GetAllAsync]
    ML --> LS2[LoanService.AddAsync]
    MR --> LS3[LoanService.RecordRepaymentAsync]
```

---

## Feature — Dashboard

```mermaid
flowchart TD
    DQ[dashboard query\nextract UserId]

    DQ --> TI[totalIncome\nSUM of all income amounts]
    DQ --> TE[totalExpenses\nSUM of all expense amounts]
    DQ --> NB[netBalance\ntotalIncome minus totalExpenses]
    DQ --> SP[savingsProgress\nlist of goals with percentage]
    DQ --> LS[loanSummary\ntotalOwed and totalGiven]
```

---

## Payload Shape

Every mutation returns a payload — never throws. Either data or an error is set, never both.

```mermaid
flowchart LR
    P[Payload]
    P --> D[data\nentity or null on failure]
    P --> E[errors\nstring list or null on success]
```

---

## Add Income — Full End-to-End Flow

```mermaid
sequenceDiagram
    participant Client
    participant JWTMiddleware
    participant Mutation
    participant IncomeService
    participant IncomeRepository
    participant SupabaseDB

    Client->>JWTMiddleware: addIncome mutation\nBearer JWT
    JWTMiddleware->>JWTMiddleware: Validate token\nextract UserId
    JWTMiddleware->>Mutation: forward with ClaimsPrincipal
    Mutation->>IncomeService: AddAsync(input, userId)
    IncomeService->>IncomeService: Validate input
    IncomeService->>IncomeRepository: AddAsync(income)
    IncomeRepository->>SupabaseDB: INSERT
    SupabaseDB-->>IncomeRepository: OK
    IncomeRepository-->>IncomeService: saved income
    IncomeService-->>Mutation: Result.Ok(income)
    Mutation-->>Client: IncomePayload with income
```

---

## Queries Reference

| Query | Auth | Returns |
|---|---|---|
| `health` | public | String |
| `incomes` | required | list of Income |
| `expenses` | required | list of Expense |
| `savingsGoals` | required | list of SavingsGoal |
| `loans` | required | list of Loan |
| `dashboard` | required | DashboardSummary |

---

## Mutations Reference

| Mutation | Auth | Input | Returns |
|---|---|---|---|
| `signUp` | public | `SignUpInput` | `AuthPayload` |
| `signIn` | public | `SignInInput` | `AuthPayload` |
| `addIncome` | required | `AddIncomeInput` | `IncomePayload` |
| `addExpense` | required | `AddExpenseInput` | `ExpensePayload` |
| `createSavingsGoal` | required | `CreateSavingsGoalInput` | `SavingsGoalPayload` |
| `addLoan` | required | `AddLoanInput` | `LoanPayload` |
| `recordRepayment` | required | `RecordRepaymentInput` | `LoanPayload` |

---

## SOLID and Design Rules

| Rule | How it applies |
|---|---|
| Single Responsibility | Resolvers only extract UserId and delegate to services |
| Dependency Inversion | Resolvers depend on service interfaces, not implementations |
| DRY | UserId extraction shared via a helper or base class |
| Open / Closed | New features add new resolver methods — existing ones unchanged |

---

## Exit Criteria

- All queries return data scoped to the authenticated user
- All mutations persist records and return the entity in the payload
- Unauthenticated requests to protected operations return `401`
- `health` query works without a token
- Dashboard returns correct aggregated totals
- `Result.Fail` from a service returns an error payload — never throws
