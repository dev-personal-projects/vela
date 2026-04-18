# 📋 Product Requirements Document (PRD)
## Finance Tracker — GraphQL + .NET + Kotlin

---

## 1. Overview

Finance Tracker is a personal finance management system that helps users take control of their money. It provides a structured way to record income, track expenses, manage savings goals, and monitor loans — all through a clean GraphQL API backed by Supabase (PostgreSQL + Auth).

---

## 2. Goals

- Give users a clear picture of their financial health
- Make it easy to record and categorize financial activity
- Track progress toward savings goals
- Monitor loan obligations and repayments

---

## 3. User Stories

### 🔐 Authentication
- As a user, I want to sign up with my email and password
- As a user, I want to log in and receive a JWT token to access my data
- As a user, I want my financial data to be private and only accessible to me

### 💰 Income
- As a user, I want to add an income record so I can track money I receive
- As a user, I want to filter income by date or source so I can review my earnings

### 💸 Expenses
- As a user, I want to add an expense so I can track where my money goes
- As a user, I want to categorize expenses so I can understand my spending patterns
- As a user, I want to filter expenses by date and category

### 🏦 Savings
- As a user, I want to create a savings goal so I can work toward a target
- As a user, I want to see my progress toward each savings goal

### 💳 Loans
- As a user, I want to record money I owe so I can track my liabilities
- As a user, I want to record money I lent so I can track what others owe me
- As a user, I want to see repayment progress for each loan

### 📊 Dashboard
- As a user, I want to see my total income, expenses, and net balance at a glance
- As a user, I want a summary of my savings and loan status

---

## 4. Feature Map

```mermaid
mindmap
  root((Finance Tracker))
    Income
      Add income
      Filter by date
      Filter by source
    Expenses
      Add expense
      Categorize
      Filter by date
      Filter by category
    Savings
      Create goal
      Track progress
    Loans
      Loans Owed
        Add liability
        Track repayment
      Loans Given
        Add asset
        Track repayment
    Dashboard
      Net balance
      Total income
      Total expenses
      Savings summary
      Loan summary
```

---

## 5. Core Entities

```mermaid
erDiagram
    AUTH_USERS {
        uuid id PK
        string email
    }

    INCOME {
        uuid id PK
        uuid user_id FK
        decimal amount
        string source
        string description
        datetime date
    }

    EXPENSE {
        uuid id PK
        uuid user_id FK
        decimal amount
        string category
        string description
        datetime date
    }

    SAVINGS_GOAL {
        uuid id PK
        uuid user_id FK
        string name
        decimal target_amount
        decimal current_amount
        datetime deadline
    }

    LOAN {
        uuid id PK
        uuid user_id FK
        string counterparty_name
        decimal amount
        decimal amount_repaid
        string type
        string description
        datetime date
        datetime due_date
    }

    AUTH_USERS ||--o{ INCOME : "owns"
    AUTH_USERS ||--o{ EXPENSE : "owns"
    AUTH_USERS ||--o{ SAVINGS_GOAL : "owns"
    AUTH_USERS ||--o{ LOAN : "owns"
```

> `AUTH_USERS` represents `auth.users` — managed entirely by Supabase. We never create or modify it.  
> `user_id` on every table is a foreign key to `auth.users.id`.  
> `LoanType` is an enum: `Owed` (liability) | `Given` (asset)

---

## 6. Dashboard Summary

```mermaid
flowchart LR
    I[Income Records] --> D{Dashboard}
    E[Expense Records] --> D
    S[Savings Goals] --> D
    L[Loans] --> D

    D --> NB[Net Balance\nIncome - Expenses]
    D --> SP[Savings Progress]
    D --> LS[Loan Summary\nOwed vs Given]
```

---

## 7. Out of Scope (MVP)

| Feature | Reason |
|---|---|
| Payment integrations | Complexity, out of MVP scope |
| Bank syncing | Requires third-party APIs |
| Notifications | Future phase |
| Advanced analytics | Future phase |
| Multi-user / shared accounts | Future phase |

---

## 8. Future Roadmap

```mermaid
timeline
    title Finance Tracker Roadmap
    Phase 1 : Backend API
            : Domain models
            : GraphQL queries & mutations
            : Core finance features
    Phase 2 : Mobile App
            : Kotlin + Apollo GraphQL
            : Offline support
    Phase 3 : Advanced Features
            : Notifications & reminders
            : Analytics & reports
            : Event-driven features
```

---

## 9. Success Criteria

- Users can register and log in via Supabase Auth
- All GraphQL operations are protected — unauthenticated requests are rejected
- Each user can only access their own financial data
- All MVP features accessible via GraphQL API
- Clean, validated mutations with meaningful error responses
- Dashboard query returns accurate aggregated data
- Mobile app (Phase 2) can consume the API with no changes to the backend
