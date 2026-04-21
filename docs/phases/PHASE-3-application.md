# Phase 3 — Application Layer

## Goal
Implement all business logic through services. Services orchestrate domain entities, call repositories, validate inputs, and return `Result<T>`. No GraphQL, no EF Core, no HTTP concerns here.

---

## Checklist

- [x] DTOs defined for all inputs
- [x] `IAuthService` interface and `AuthService` implementation
- [x] `IIncomeService` interface and `IncomeService` implementation
- [x] `IExpenseService` interface and `ExpenseService` implementation
- [x] `ISavingsGoalService` interface and `SavingsGoalService` implementation
- [x] `ILoanService` interface and `LoanService` implemented
- [x] All services return `AppResult<T>` or `AppResult`
- [x] All validation rules enforced in services
- [x] All services registered in DI

---

## Folder Structure

```
Application/
├── Interfaces/
│   ├── IAuthService.cs
│   ├── IIncomeRepository.cs        Phase 2
│   ├── IExpenseRepository.cs       Phase 2
│   ├── ISavingsGoalRepository.cs   Phase 2
│   ├── ILoanRepository.cs          Phase 2
│   ├── IIncomeService.cs
│   ├── IExpenseService.cs
│   ├── ISavingsGoalService.cs
│   └── ILoanService.cs
├── Services/
│   ├── AuthService.cs
│   ├── IncomeService.cs
│   ├── ExpenseService.cs
│   ├── SavingsGoalService.cs
│   └── LoanService.cs
└── DTOs/
    ├── Auth/
    │   ├── SignUpInput.cs
    │   ├── SignInInput.cs
    │   └── AuthPayload.cs
    ├── Income/
    │   └── AddIncomeInput.cs
    ├── Expense/
    │   └── AddExpenseInput.cs
    ├── Savings/
    │   └── CreateSavingsGoalInput.cs
    └── Loan/
        ├── AddLoanInput.cs
        └── RecordRepaymentInput.cs
```

---

## Feature — Auth Service

```mermaid
flowchart LR
    AS[IAuthService]
    AS --> SU[SignUpAsync - email password
    calls ISupabaseAuthClient
    returns Result of AuthPayload]
    AS --> SI[SignInAsync - email password
    calls ISupabaseAuthClient
    returns Result of AuthPayload]

    SU --> V1[Email must be valid format]
    SU --> V2[Password minimum 8 characters]
```

> `AuthService` depends on `ISupabaseAuthClient` — an interface wrapping the Supabase HTTP calls. This keeps `AuthService` testable and decoupled from the HTTP client (Dependency Inversion).

---

## Service Flow — All Features

```mermaid
flowchart TD
    RESOLVER[GraphQL Resolver\npasses input and UserId]
    RESOLVER --> SVC[Service method]

    SVC --> VAL{Validate input}
    VAL -- invalid --> FAIL[Result.Fail\nerror message]
    VAL -- valid --> BUILD[Build domain entity]
    BUILD --> REPO[Repository call]
    REPO --> OK[Result.Ok\nentity]

    FAIL --> ERR[GraphQL error payload]
    OK --> DATA[GraphQL data payload]
```

---

## Feature — Income Service

```mermaid
flowchart LR
    IS[IIncomeService]
    IS --> GA["GetAllAsync(userId)\nResult of list of Income"]
    IS --> AD["AddAsync(input, userId)\nResult of Income"]

    AD --> V1[Amount must be greater than 0]
    AD --> V2[Source must not be empty]
    AD --> V3[Date must not be in the future]
```

---

## Feature — Expense Service

```mermaid
flowchart LR
    ES[IExpenseService]
    ES --> GA["GetAllAsync(userId)\nResult of list of Expense"]
    ES --> AD["AddAsync(input, userId)\nResult of Expense"]

    AD --> V1[Amount must be greater than 0]
    AD --> V2[Category must not be empty]
    AD --> V3[Date must not be in the future]
```

---

## Feature — Savings Goal Service

```mermaid
flowchart LR
    SS[ISavingsGoalService]
    SS --> GA["GetAllAsync(userId)\nResult of list of SavingsGoal"]
    SS --> CR["CreateAsync(input, userId)\nResult of SavingsGoal"]
    SS --> UP["UpdateProgressAsync(id, amount, userId)\nResult of SavingsGoal"]

    CR --> V1[TargetAmount must be greater than 0]
    CR --> V2[Name must not be empty]
    UP --> V3[New amount must not exceed TargetAmount]
```

---

## Feature — Loan Service

```mermaid
flowchart LR
    LS[ILoanService]
    LS --> GA["GetAllAsync(userId)\nResult of list of Loan"]
    LS --> AD["AddAsync(input, userId)\nResult of Loan"]
    LS --> RP["RecordRepaymentAsync(input, userId)\nResult of Loan"]

    AD --> V1[Amount must be greater than 0]
    AD --> V2[CounterpartyName must not be empty]
    AD --> V3[DueDate must be after Date]
    RP --> V4[Repayment must not exceed RemainingBalance]
```

---

## Feature — Loan Repayment Flow

```mermaid
sequenceDiagram
    participant Service
    participant Repository

    Service->>Repository: GetByIdAsync(loanId, userId)
    Repository-->>Service: Loan or null

    alt Loan not found
        Service-->>Resolver: Result.Fail - Loan not found
    else Repayment exceeds balance
        Service-->>Resolver: Result.Fail - Exceeds remaining balance
    else Valid repayment
        Service->>Service: loan.AmountRepaid += amount
        Service->>Repository: UpdateAsync(loan)
        Repository-->>Service: updated loan
        Service-->>Resolver: Result.Ok - updated loan
    end
```

---

## Validation Rules

| Field | Rule |
|---|---|
| `Amount` | Must be greater than 0 |
| `Date` | Must not be in the future for income and expenses |
| `Source` | Must not be empty |
| `Category` | Must not be empty |
| `Name` | Must not be empty |
| `TargetAmount` | Must be greater than 0 |
| `CurrentAmount` | Must be between 0 and TargetAmount |
| `DueDate` | Must be after `Date` |
| `RepaymentAmount` | Must not exceed `RemainingBalance` |
| `CounterpartyName` | Must not be empty |

---

## SOLID and Design Rules

| Rule | How it applies |
|---|---|
| Single Responsibility | Each service handles one feature domain only |
| Open / Closed | New features add new services — existing ones are not modified |
| Liskov Substitution | Service interfaces can be swapped for test doubles |
| Interface Segregation | One interface per service — no fat interfaces |
| Dependency Inversion | Services depend on repository interfaces, not implementations |
| DRY | Shared validation helpers extracted if used across services |

---

## Exit Criteria

- All services compile and resolve via DI
- Valid inputs produce `Result.Ok` with the entity
- Invalid inputs produce `Result.Fail` with a clear message
- Repayment exceeding balance returns `Result.Fail`
- No EF Core, GraphQL, or HTTP references anywhere in this layer
