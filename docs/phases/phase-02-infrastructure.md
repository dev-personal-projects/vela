# Phase 2 — Infrastructure Layer

## Goal
Connect to Supabase PostgreSQL via EF Core, apply the SQL migrations, and implement all repositories. Repositories are the only place that touches the database.

---

## Checklist

- [x] `AppDbContext` configured with all entity `DbSet`s and table mappings
- [x] EF Core column mappings match the SQL migration schema
- [x] SQL migrations applied to Supabase via `apply-migrations.sh`
- [x] `IIncomeRepository` interface defined in Application layer
- [x] `IExpenseRepository` interface defined in Application layer
- [x] `ISavingsGoalRepository` interface defined in Application layer
- [x] `ILoanRepository` interface defined in Application layer
- [x] `ILoanRepaymentRepository` interface defined in Application layer
- [x] All repository implementations complete
- [x] All repositories registered in DI in `Program.cs`

---

## Folder Structure

```
Application/
└── Interfaces/
    ├── IIncomeRepository.cs
    ├── IExpenseRepository.cs
    ├── ISavingsGoalRepository.cs
    ├── ILoanRepository.cs
    └── ILoanRepaymentRepository.cs

Infrastructure/
├── Data/
│   └── AppDbContext.cs
└── Repositories/
    ├── IncomeRepository.cs
    ├── ExpenseRepository.cs
    ├── SavingsGoalRepository.cs
    ├── LoanRepository.cs
    └── LoanRepaymentRepository.cs

scripts/
└── db/
    ├── 000_create_profiles.sql         done
    ├── 001_create_incomes.sql          done
    ├── 002_create_expenses.sql         done
    ├── 003_create_savings_goals.sql    done
    ├── 004_create_loans.sql            done
    ├── 005_add_loan_counterparty_phone_number.sql done
    ├── 006_create_loan_repayments.sql  done
    └── rollback/
        ├── rollback_profiles.sql       done
        ├── rollback_loans.sql          done
        ├── rollback_loan_repayments.sql done
        ├── rollback_savings_goals.sql  done
        ├── rollback_expenses.sql       done
        └── rollback_incomes.sql        done
```

---

## Architecture

```mermaid
flowchart TB
    subgraph Application
        IR[IIncomeRepository]
        ER[IExpenseRepository]
        SR[ISavingsGoalRepository]
        LR[ILoanRepository]
        LRR[ILoanRepaymentRepository]
    end

    subgraph Infrastructure
        IMP1[IncomeRepository]
        IMP2[ExpenseRepository]
        IMP3[SavingsGoalRepository]
        IMP4[LoanRepository]
        IMP5[LoanRepaymentRepository]
        CTX[AppDbContext]
    end

    subgraph Supabase
        DB[(PostgreSQL)]
    end

    IR -.implemented by.-> IMP1
    ER -.implemented by.-> IMP2
    SR -.implemented by.-> IMP3
    LR -.implemented by.-> IMP4
    LRR -.implemented by.-> IMP5

    IMP1 --> CTX
    IMP2 --> CTX
    IMP3 --> CTX
    IMP4 --> CTX
    IMP5 --> CTX
    CTX --> DB
```

---

## Database Schema

```mermaid
erDiagram
    incomes {
        uuid id PK
        uuid user_id FK
        numeric amount
        text source
        text description
        timestamptz date
        timestamptz created_at
    }

    expenses {
        uuid id PK
        uuid user_id FK
        numeric amount
        text category
        text description
        timestamptz date
        timestamptz created_at
    }

    savings_goals {
        uuid id PK
        uuid user_id FK
        text name
        numeric target_amount
        numeric current_amount
        timestamptz deadline
        timestamptz created_at
    }

    loans {
        uuid id PK
        uuid user_id FK
        text counterparty_name
        text counterparty_phone_number
        numeric amount
        numeric amount_repaid
        loan_type type
        text description
        timestamptz date
        timestamptz due_date
        timestamptz created_at
    }

    loan_repayments {
        uuid id PK
        uuid loan_id FK
        numeric amount
        timestamptz date
        text note
        timestamptz created_at
    }

    loans ||--o{ loan_repayments : has_many
```

---

## Feature — Income Repository

```mermaid
flowchart LR
    IR[IIncomeRepository]
    IR --> GA["GetAllAsync(userId)\nreturns list of Income"]
    IR --> GI["GetByIdAsync(id, userId)\nreturns Income or null"]
    IR --> AD["AddAsync(income)\nvoid"]
    IR --> DL["DeleteAsync(id, userId)\nvoid"]
```

---

## Feature — Expense Repository

```mermaid
flowchart LR
    ER[IExpenseRepository]
    ER --> GA["GetAllAsync(userId)\nreturns list of Expense"]
    ER --> GI["GetByIdAsync(id, userId)\nreturns Expense or null"]
    ER --> AD["AddAsync(expense)\nvoid"]
    ER --> DL["DeleteAsync(id, userId)\nvoid"]
```

---

## Feature — Savings Goal Repository

```mermaid
flowchart LR
    SR[ISavingsGoalRepository]
    SR --> GA["GetAllAsync(userId)\nreturns list of SavingsGoal"]
    SR --> GI["GetByIdAsync(id, userId)\nreturns SavingsGoal or null"]
    SR --> AD["AddAsync(goal)\nvoid"]
    SR --> UP["UpdateAsync(goal)\nvoid"]
```

---

## Feature — Loan Repository

```mermaid
flowchart LR
    LR[ILoanRepository]
    LR --> GA["GetAllAsync(userId, type?)\nreturns list of Loan"]
    LR --> GI["GetByIdAsync(id, userId)\nreturns Loan or null"]
    LR --> AD["AddAsync(loan)\nvoid"]
    LR --> UP["UpdateAsync(loan)\nvoid"]
    LR --> DL["DeleteAsync(id, userId)\nreturns bool"]
```

---

## Feature — Loan Repayment Repository

```mermaid
flowchart LR
    LRR[ILoanRepaymentRepository]
    LRR --> GA["GetByLoanIdAsync(loanId)\nreturns list of LoanRepayment"]
    LRR --> AD["AddAsync(repayment)\nvoid"]
```

---

## UserId Scoping Rule

```mermaid
flowchart LR
    REQ[Request\nUserId from JWT]
    REPO[Repository method]
    FILTER[WHERE user_id = UserId]
    DB[(Supabase DB)]
    DATA[Only this user's data]

    REQ --> REPO
    REPO --> FILTER
    FILTER --> DB
    DB --> DATA
```

> Every query and command filters by `UserId`. Cross-user data access is impossible at the repository level.

---

## Migration Workflow

```mermaid
flowchart LR
    SQL[scripts/db/\n000 to 006 .sql files]
    VAL[validate-migrations.sh\ndry-run against Supabase]
    APP[apply-migrations.sh\napply to Supabase]
    DB[(Supabase\nPostgreSQL)]

    SQL --> VAL
    VAL -- passes --> APP
    APP --> DB
```

---

## SOLID and Design Rules

| Rule | How it applies |
|---|---|
| Single Responsibility | Each repository handles one entity only |
| Interface Segregation | One interface per repository — no fat interfaces |
| Dependency Inversion | Application defines interfaces, Infrastructure implements them |
| DRY | Common query patterns shared via base repository if needed |
| Open / Closed | New features add new repositories — existing ones unchanged |

---

## Exit Criteria

- SQL migrations apply cleanly via `apply-migrations.sh`
- `validate-migrations.sh` passes with no errors
- Core tables plus `loan_repayments` exist in Supabase with correct columns and constraints
- Repositories return only records belonging to the requesting user
- DI resolves all repository interfaces to their implementations
