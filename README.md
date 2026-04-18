# 💸 Finance Tracker (GraphQL + .NET + Kotlin)

## 📌 Overview

This project is a **personal finance tracking system** designed to help users manage and understand their money through a simple and structured approach.

It allows users to track:

* Income
* Expenses
* Savings
* Loans (money owed and money lent)

The system is being built primarily to explore:

* GraphQL API design
* Clean Architecture in .NET
* Mobile-backend integration (Kotlin – later phase)

---

## 🧱 Project Structure

```
/backend   → .NET GraphQL API (current focus)
/mobile    → Kotlin mobile app (planned)
/docs      → Project documentation and notes
README.md
```

---

## 🚧 Current Focus: Backend

We are currently building the **backend API** using:

* .NET (Clean Architecture)
* GraphQL (Hot Chocolate)

### Goals:

* Design a clean and scalable API
* Model real-world financial data
* Implement core finance features

---

## 🎯 Core Features (MVP)

### 💰 Income

* Add income records
* View and filter income

### 💸 Expenses

* Add expenses
* Categorize spending
* Filter by date and category

### 🏦 Savings

* Create savings goals
* Track progress toward goals

### 💳 Loans

#### Loans Owed (Liabilities)

* Track borrowed money
* Monitor repayment progress

#### Loans Given (Assets)

* Track money lent to others
* Monitor repayment status

### 📊 Dashboard

* Total income
* Total expenses
* Net balance
* Savings progress
* Loan summaries

---

## ⚙️ Architecture

The backend follows **Clean Architecture principles**:

```
Domain        → Core business models
Application   → Business logic & services
Infrastructure→ Database & repositories
API           → GraphQL layer
```

### Key Rules:

* Business logic lives in services
* Data access via repositories
* GraphQL handles request/response only

---

## 📡 API Design (GraphQL)

The API will expose:

* Queries → Fetch data
* Mutations → Create/update data

Examples:

* Add income
* Add expense
* Create savings goal
* Manage loans

---

## 🚫 Out of Scope (for now)

* Payment integrations
* Bank syncing
* Notifications
* Advanced analytics
* Multi-user/shared accounts

---

## 🚀 Future Improvements

* Mobile app (Kotlin + Apollo GraphQL)
* Notifications (loan reminders, alerts)
* Analytics and reports
* Offline support
* Event-driven features

---

## 🧠 Purpose

This project is designed to:

* Build strong backend architecture skills
* Learn GraphQL in a real-world context
* Practice designing scalable systems

---

## 🏁 Status

🚧 Backend development in progress
📱 Mobile app planned for later phase
