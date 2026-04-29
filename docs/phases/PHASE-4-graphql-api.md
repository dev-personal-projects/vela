# Phase 4 — GraphQL API Layer

## Goal
Expose all features through a clean, authenticated GraphQL API. Resolvers are thin — extract `UserId` from the JWT, call the service, return the result. No business logic lives here.

---

## Checklist

- [x] JWT Bearer + Hot Chocolate authorization wired in `Program.cs`
- [x] `signUp` mutation calls Supabase Auth and returns a success payload
- [x] `signIn` mutation calls Supabase Auth and returns JWT
- [x] All data queries protected with `[Authorize]`
- [x] All mutations except `signUp` and `signIn` protected with `[Authorize]`
- [x] `UserId` extracted from `ClaimsPrincipal` in every protected resolver
- [x] `health` query remains public
- [x] All mutations return typed payloads with data or error
- [x] Dashboard query returns aggregated summary

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
        └── AuthDtos.cs
```

---

## Full API Map

```mermaid
flowchart TB
    subgraph Queries
        QH[health\npublic]
        QI[incomes\noptional fromDate / toDate / source\nauthenticated]
        QE[expenses\noptional fromDate / toDate / category\nauthenticated]
        QS[savingsGoals\nauthenticated]
        QL[loans\noptional type\nauthenticated]
        QD[dashboard\nauthenticated]
        QI1[income by id\nauthenticated]
        QE1[expense by id\nauthenticated]
        QS1[savingsGoal by id\nauthenticated]
        QL1[loan by id\nauthenticated]
    end

    subgraph Mutations
        MSU[signUp\npublic]
        MSI[signIn\npublic]
        MI[addIncome\nauthenticated]
        MIU[updateIncome\nauthenticated]
        MID[deleteIncome\nauthenticated]
        ME[addExpense\nauthenticated]
        MEU[updateExpense\nauthenticated]
        MED[deleteExpense\nauthenticated]
        MS[createSavingsGoal\nauthenticated]
        MSU2[updateSavingsGoal\nauthenticated]
        MSC[addSavingsContribution\nauthenticated]
        MSP[updateSavingsProgress\nauthenticated]
        MSD[deleteSavingsGoal\nauthenticated]
        ML[addLoan\nauthenticated]
        MLU[updateLoan\nauthenticated]
        MR[recordRepayment\nauthenticated]
        MLD[deleteLoan\nauthenticated]
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
        MSU[signUp\nSignUpInput\nreturns SignUpPayloadResult]
        MSI[signIn\nSignInInput\nreturns SignInPayloadResult]
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
    SupabaseAuth-->>AuthService: auth result
    AuthService-->>Mutation: Result.Ok(message)
    Mutation-->>Client: SignUpPayloadResult with message
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
    AuthService-->>Mutation: Result.Ok(signInPayload)
    Mutation-->>Client: SignInPayloadResult with accessToken
```

---

## Feature — Income

```mermaid
flowchart LR
    subgraph Query
        QI[incomes\noptional fromDate / toDate / source\nreturns list of Income]
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
        QE[expenses\noptional fromDate / toDate / category\nreturns list of Expense]
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
        MSC[addSavingsContribution\nAddSavingsContributionInput\nreturns SavingsGoalPayload]
    end

    QS --> SS[SavingsGoalService.GetAllAsync]
    MS --> SS2[SavingsGoalService.CreateAsync]
    MSC --> SS3[SavingsGoalService.AddContributionAsync]
```

---

## Feature — Loans

```mermaid
flowchart LR
    subgraph Query
        QL[loans\noptional type / status\nreturns list of Loan]
        QR[loanRepaymentHistory\nreturns list of LoanRepayment]
    end

    subgraph Mutations
        ML[addLoan\nAddLoanInput\nreturns LoanPayload]
        MR[recordRepayment\nRecordRepaymentInput\nreturns LoanPayload]
        MSL[settleLoan\nSettleLoanInput\nreturns LoanPayload]
    end

    QL --> LS[LoanService.GetAllAsync]
    QR --> LS4[LoanService.GetRepaymentHistoryAsync]
    ML --> LS2[LoanService.AddAsync]
    MR --> LS3[LoanService.RecordRepaymentAsync]
    MSL --> LS5[LoanService.SettleAsync]
```

---

## Feature — Dashboard

```mermaid
flowchart TD
    DQ[dashboard query\nextract UserId]

    DQ --> TI[totalIncome\nSUM of all income amounts]
    DQ --> TE[totalExpenses\nSUM of all expense amounts]
    DQ --> NB[netBalance\ntotalIncome minus totalExpenses]
    DQ --> SP[savingsProgress\nlist of goals with current / remaining / percentage]
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
| `incomes(fromDate?, toDate?, source?)` | required | list of Income |
| `income(id)` | required | Income or null |
| `expenses(fromDate?, toDate?, category?)` | required | list of Expense |
| `expense(id)` | required | Expense or null |
| `savingsGoals` | required | list of SavingsGoal |
| `savingsGoal(id)` | required | SavingsGoal or null |
| `loans(type?, status?)` | required | list of Loan |
| `loan(id)` | required | Loan or null |
| `loanRepaymentHistory(loanId)` | required | list of LoanRepayment |
| `dashboard` | required | DashboardSummary |

---

## Mutations Reference

| Mutation | Auth | Input | Returns |
|---|---|---|---|
| `signUp` | public | `SignUpInput` | `SignUpPayloadResult` |
| `signIn` | public | `SignInInput` | `SignInPayloadResult` |
| `addIncome` | required | `AddIncomeInput` | `IncomePayload` |
| `updateIncome` | required | `UpdateIncomeInput` | `IncomePayload` |
| `deleteIncome` | required | `id` | `DeletePayload` |
| `addExpense` | required | `AddExpenseInput` | `ExpensePayload` |
| `updateExpense` | required | `UpdateExpenseInput` | `ExpensePayload` |
| `deleteExpense` | required | `id` | `DeletePayload` |
| `createSavingsGoal` | required | `CreateSavingsGoalInput` | `SavingsGoalPayload` |
| `updateSavingsGoal` | required | `UpdateSavingsGoalInput` | `SavingsGoalPayload` |
| `addSavingsContribution` | required | `AddSavingsContributionInput` | `SavingsGoalPayload` |
| `updateSavingsProgress` | required | `id`, `newAmount` | `SavingsGoalPayload` |
| `deleteSavingsGoal` | required | `id` | `DeletePayload` |
| `addLoan` | required | `AddLoanInput` | `LoanPayload` |
| `updateLoan` | required | `UpdateLoanInput` | `LoanPayload` |
| `recordRepayment` | required | `RecordRepaymentInput` | `LoanPayload` |
| `settleLoan` | required | `SettleLoanInput` | `LoanPayload` |
| `deleteLoan` | required | `id` | `DeletePayload` |

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
