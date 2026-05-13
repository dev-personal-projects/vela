# Phase 5 — Testing and Hardening

## Goal
Validate the entire backend end-to-end. Cover every service's happy and failure paths, verify repository data scoping, test GraphQL resolvers, and confirm no data leaks between users.

## Test Plan
Use the detailed plan for what to test, where to test it, and exit criteria:
- `docs/test-plan.md`

---

## Checklist

- [ ] Unit tests for all service happy paths
- [ ] Unit tests for all service failure paths
- [ ] Repository integration tests against Supabase or local PostgreSQL
- [ ] GraphQL query and mutation integration tests
- [ ] Auth tests — unauthenticated requests rejected
- [ ] Cross-user data isolation verified for all features
- [ ] Dashboard aggregation tested with known data

---

## Test Project Structure

```
Tests/
├── Unit/
│   ├── Income/
│   │   └── IncomeServiceTests.cs
│   ├── Expense/
│   │   └── ExpenseServiceTests.cs
│   ├── Savings/
│   │   └── SavingsGoalServiceTests.cs
│   └── Loan/
│       └── LoanServiceTests.cs
└── Integration/
    ├── Income/
    │   └── IncomeRepositoryTests.cs
    ├── Expense/
    │   └── ExpenseRepositoryTests.cs
    ├── Savings/
    │   └── SavingsGoalRepositoryTests.cs
    ├── Loan/
    │   └── LoanRepositoryTests.cs
    ├── GraphQL/
    │   ├── IncomeResolverTests.cs
    │   ├── ExpenseResolverTests.cs
    │   ├── SavingsResolverTests.cs
    │   ├── LoanResolverTests.cs
    │   └── DashboardResolverTests.cs
    └── Auth/
        └── AuthorizationTests.cs
```

---

## Feature — Income Tests

```mermaid
flowchart TD
    IT[Income Tests]

    IT --> IU[Unit]
    IU --> IU1[AddAsync with valid input returns Result.Ok]
    IU --> IU2[AddAsync with amount zero returns Result.Fail]
    IU --> IU3[AddAsync with empty source returns Result.Fail]
    IU --> IU4[AddAsync with future date returns Result.Fail]

    IT --> II[Integration]
    II --> II1[AddAsync persists record to DB]
    II --> II2[GetAllAsync returns only user's records]
```

---

## Feature — Expense Tests

```mermaid
flowchart TD
    ET[Expense Tests]

    ET --> EU[Unit]
    EU --> EU1[AddAsync with valid input returns Result.Ok]
    EU --> EU2[AddAsync with amount zero returns Result.Fail]
    EU --> EU3[AddAsync with empty category returns Result.Fail]

    ET --> EI[Integration]
    EI --> EI1[AddAsync persists record to DB]
    EI --> EI2[GetAllAsync filters by userId correctly]
```

---

## Feature — Savings Goal Tests

```mermaid
flowchart TD
    ST[Savings Goal Tests]

    ST --> SU[Unit]
    SU --> SU1[CreateAsync with valid input returns Result.Ok]
    SU --> SU2[CreateAsync with target zero returns Result.Fail]
    SU --> SU3[UpdateProgressAsync exceeding target returns Result.Fail]
    SU --> SU4[Progress calculation is correct]

    ST --> SI[Integration]
    SI --> SI1[CreateAsync persists goal to DB]
    SI --> SI2[UpdateProgressAsync updates current_amount]
```

---

## Feature — Loan Tests

```mermaid
flowchart TD
    LT[Loan Tests]

    LT --> LU[Unit]
    LU --> LU1[AddAsync with valid input returns Result.Ok]
    LU --> LU2[AddAsync with due date before date returns Result.Fail]
    LU --> LU3[RecordRepayment valid amount returns Result.Ok]
    LU --> LU4[RecordRepayment exceeds balance returns Result.Fail]
    LU --> LU5[IsSettled returns true when fully repaid]

    LT --> LI[Integration]
    LI --> LI1[AddAsync persists loan to DB]
    LI --> LI2[RecordRepayment updates amount_repaid]
```

---

## Auth and Isolation Tests

```mermaid
flowchart TD
    AT[Auth Tests]

    AT --> A1[No JWT returns 401]
    AT --> A2[Invalid JWT returns 401]
    AT --> A3[Expired JWT returns 401]
    AT --> A4[Valid JWT returns 200]
    AT --> A5[health query works without JWT]

    AT --> ISO[Isolation Tests]
    ISO --> I1[User A income not visible to User B]
    ISO --> I2[User A expense not visible to User B]
    ISO --> I3[User A savings not visible to User B]
    ISO --> I4[User A loans not visible to User B]
```

---

## Data Isolation Sequence

```mermaid
sequenceDiagram
    participant UserA
    participant API
    participant DB
    participant UserB

    UserA->>API: addIncome - JWT A
    API->>DB: INSERT income - userId A
    DB-->>API: OK
    API-->>UserA: income record

    UserB->>API: incomes query - JWT B
    API->>DB: SELECT WHERE user_id = B
    DB-->>API: empty result
    API-->>UserB: empty list - no data leaked
```

---

## Dashboard Tests

```mermaid
flowchart LR
    DT[Dashboard Tests]
    DT --> D1[totalIncome equals sum of all income records]
    DT --> D2[totalExpenses equals sum of all expense records]
    DT --> D3[netBalance equals totalIncome minus totalExpenses]
    DT --> D4[savingsProgress shows correct percentage per goal]
    DT --> D5[loanSummary shows correct totalOwed and totalGiven]
```

---

## Test Tools

| Tool | Purpose |
|---|---|
| `xUnit` | Unit and integration test framework |
| `Moq` | Mock repository interfaces in unit tests |
| `Microsoft.AspNetCore.Mvc.Testing` | In-process GraphQL integration tests |
| Local PostgreSQL or Supabase | Integration test database |

---

## SOLID and Design Rules for Tests

| Rule | How it applies |
|---|---|
| Single Responsibility | Each test class covers one service or resolver only |
| DRY | Shared test data builders and fixtures — no copy-pasted setup |
| Arrange Act Assert | Every test follows AAA structure consistently |
| Isolation | Unit tests mock all dependencies — no real DB calls |

---

## Exit Criteria

- All unit tests pass
- All integration tests pass
- No cross-user data leakage in any feature
- Invalid inputs always produce `Result.Fail` with a message
- Unauthenticated requests always return `401`
- Dashboard returns correct aggregated values with known seed data
