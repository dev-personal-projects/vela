# Phase 1 — Domain Layer

## Goal
Define all core business entities and enums. This layer is the heart of the application — pure C#, zero external dependencies.

---

## Checklist

- [ ] `LoanType` enum created
- [ ] `Income` entity created
- [ ] `Expense` entity created
- [ ] `SavingsGoal` entity with `Progress()` computed method
- [ ] `Loan` entity with `RemainingBalance()` computed method
- [ ] All entities carry `UserId` (Supabase Auth UUID)
- [ ] All entities enforce invariants via constructor — no public setters on required fields

---

## Folder Structure

```
Domain/
├── Common/
│   └── Result.cs           done
├── Entities/
│   ├── Income.cs
│   ├── Expense.cs
│   ├── SavingsGoal.cs
│   └── Loan.cs
└── Enums/
    └── LoanType.cs
```

---

## Entities

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
        +DateTime Deadline
        +DateTime CreatedAt
        +decimal Progress()
    }

    class Loan {
        +Guid Id
        +Guid UserId
        +string CounterpartyName
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

---

## Feature — Income

```mermaid
flowchart LR
    INC[Income entity]
    INC --> AM[Amount\nmust be greater than 0]
    INC --> SRC[Source\nmust not be empty]
    INC --> DT[Date\nrecorded date]
    INC --> UID[UserId\nScopes to owner]
```

---

## Feature — Expenses

```mermaid
flowchart LR
    EXP[Expense entity]
    EXP --> AM[Amount\nmust be greater than 0]
    EXP --> CAT[Category\nmust not be empty]
    EXP --> DT[Date\nrecorded date]
    EXP --> UID[UserId\nScopes to owner]
```

---

## Feature — Savings Goals

```mermaid
flowchart LR
    SG[SavingsGoal entity]
    SG --> TA[TargetAmount\nmust be greater than 0]
    SG --> CA[CurrentAmount\n0 to TargetAmount]
    SG --> PR["Progress()\nCurrentAmount divided by TargetAmount times 100"]
    SG --> DL[Deadline\noptional target date]
```

---

## Feature — Loans

```mermaid
flowchart TB
    LN[Loan entity]

    LN --> LT{LoanType}
    LT --> OW[Owed\nmoney I borrowed\nliability]
    LT --> GV[Given\nmoney I lent\nasset]

    LN --> RB["RemainingBalance()\nAmount minus AmountRepaid"]
    LN --> IS["IsSettled()\nAmountRepaid equals Amount"]
    LN --> DD[DueDate\nmust be after Date]
```

---

## Result Pattern

```mermaid
flowchart TB
    RB[ResultBase
    Error
    IsSuccess
    IsFailure]

    RB --> ROK[Result.Ok
    IsSuccess = true]
    RB --> RFL[Result.Fail
    IsFailure = true]
    RB --> TOK[Result of T .Ok - value
    Value is set]
    RB --> TFL[Result of T .Fail - error
    Error message is set]
```

> `ResultBase` holds all shared properties. `Result` and `Result<T>` extend it — no duplicated code.

---

## SOLID and Design Rules

| Rule | How it applies |
|---|---|
| Single Responsibility | Each entity owns only its own data and computed properties |
| Open / Closed | New features add new entities — existing ones are not modified |
| Dependency Inversion | Domain has no dependencies — all other layers depend on it |
| DRY | `Result<T>` and `Result` share no duplicated logic |
| No EF Core | Zero ORM attributes or references in this layer |
| No public setters | Required fields set via constructor only |

---

## Exit Criteria

- All entities compile with no external references
- `Progress()` returns the correct percentage value
- `RemainingBalance()` returns the correct remaining amount
- `IsSettled()` returns true when fully repaid
- `Result.Ok` and `Result.Fail` behave correctly
