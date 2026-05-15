# Dashboard Plan
## Finance Tracker Dashboard Strategy

---

## 1. Purpose

This document defines the plan for designing and implementing the Finance Tracker dashboard before UI work begins.

The goal is to make the dashboard:
- useful at a glance
- fast to scan
- emotionally clear, not just data-dense
- aligned with the current backend capabilities
- ready for a strong mobile experience

---

## 2. Dashboard Mission

The dashboard should answer the user’s most important questions in a few seconds:

- How much money came in?
- How much money went out?
- What is my current net position?
- How close am I to my savings goals?
- What loan balances do I still owe, and what is owed to me?
- What action should I take next?

This means the dashboard is not just a reporting screen. It is a decision-making screen.

---

## 3. Product Goals

- Give users a clear financial snapshot with minimal effort.
- Reduce the need to open multiple screens just to understand current status.
- Encourage healthy actions such as saving, recording expenses, and following up on loans.
- Make the first screen feel personal, motivating, and trustworthy.

---

## 4. Current Backend Support

The current backend already supports a strong first dashboard iteration.

### Existing dashboard query

Current GraphQL `dashboard` returns:
- `totalIncome`
- `totalExpenses`
- `netBalance`
- `period`
  - `fromDate`
  - `toDate`
  - `isFiltered`
- `insights`
  - `expenseToIncomeRatio`
  - `hasNegativeNetBalance`
  - `hasOverdueLoans`
- `savingsOverview`
  - `totalGoals`
  - `completedGoals`
  - `focusGoal`
- `savingsProgress[]`
  - `id`
  - `name`
  - `targetAmount`
  - `currentAmount`
  - `remainingAmount`
  - `progress`
- `loanSummary`
  - `totalOwed`
  - `totalGiven`
  - `activeLoanCount`
  - `overdueLoanCount`
  - `settledLoanCount`
- `recentActivity[]`
  - `referenceId`
  - `type`
  - `title`
  - `subtitle`
  - `amount`
  - `happenedAt`

Source:
- [Query.cs](/workspaces/vela/Api/GraphQL/Query.cs:146)
- [DashboardDtos.cs](/workspaces/vela/Application/DTOs/Dashboard/DashboardDtos.cs:1)

### Additional supporting queries already available

These can power richer dashboard sections when needed:
- `incomes(fromDate?, toDate?, source?)`
- `expenses(fromDate?, toDate?, category?)`
- `savingsGoals`
- `loans(type?, status?)`
- `loanRepaymentHistory(loanId)`

This means the dashboard can start with one summary query and later expand into richer drill-downs without backend redesign.

---

## 5. Primary Users

### User Type A — Daily Tracker
- wants quick totals
- checks whether spending is under control
- wants recent clarity, not deep analysis

### User Type B — Goal-Driven Saver
- wants progress and remaining amount
- needs encouragement and visibility into milestones

### User Type C — Loan Manager
- wants to know what is owed and what is due
- benefits from status-based actions like follow-up or settle

---

## 6. Dashboard UX Principles

- Prioritize clarity over decoration.
- Show the most important financial signals first.
- Use progressive disclosure: summary first, detail on tap.
- Make actions visible near the data they relate to.
- Prefer “what this means” over raw numbers alone.
- Design for low-friction mobile scanning with strong hierarchy.
- Use positive motivation for savings and neutral clarity for debt.

---

## 7. Recommended Information Architecture

Recommended dashboard structure:

1. Top summary strip
- total income
- total expenses
- net balance

2. Primary action area
- add income
- add expense
- add savings contribution
- record loan repayment

3. Savings section
- goal cards
- progress bars
- remaining amount
- nearest or highest-priority goal first

4. Loans section
- amount you owe
- amount owed to you
- active / overdue / settled breakdown
- optional “needs attention” loans

5. Insight / trend section
- lightweight guidance such as:
  - “Expenses are close to income”
  - “You are 80% toward Emergency Fund”
  - “One loan is overdue”

6. Drill-down entry points
- view all expenses
- view all savings goals
- view all loans

---

## 8. Recommended Dashboard Modules

### Module A — Financial Snapshot
Purpose:
- immediate orientation

Fields:
- `totalIncome`
- `totalExpenses`
- `netBalance`

UX notes:
- `netBalance` should be the visual focal point
- income and expenses should support comparison, not compete equally
- use short labels and easy currency formatting

### Module B — Savings Progress
Purpose:
- make goals feel tangible

Fields:
- `name`
- `targetAmount`
- `currentAmount`
- `remainingAmount`
- `progress`

UX notes:
- show percentage and remaining amount together
- highlight the next meaningful milestone
- allow direct “Add Contribution” action from each goal card

### Module C — Loan Summary
Purpose:
- separate liabilities from receivables clearly

Fields:
- `loanSummary.totalOwed`
- `loanSummary.totalGiven`

Optional richer view from `loans(type?, status?)`:
- active loans
- overdue loans
- settled loans
- per-loan remaining balance

UX notes:
- label clearly as:
  - `You owe`
  - `Owed to you`
- avoid ambiguous language

### Module D — Smart Actions
Purpose:
- reduce navigation friction

Actions:
- add expense
- add income
- add savings contribution
- add loan
- record repayment

UX notes:
- actions should be one tap away from the dashboard
- the dashboard should feel like a control center, not only a report

### Module E — Helpful Insights
Purpose:
- turn raw data into guidance

Examples:
- “Your expenses are 78% of your income.”
- “You still need UGX 8,500 to complete Emergency Fund.”
- “You have 1 overdue loan.”

Important:
- keep insights short, clear, and factual
- avoid judgmental tone

---

## 9. Mobile-First Design Guidance

- Design for vertical stacking first.
- Keep the first viewport focused on the top summary plus one action row.
- Use cards with strong spacing and obvious tap targets.
- Make numeric values legible with strong size contrast.
- Avoid overcrowding with too many simultaneous charts in v1.
- Keep drill-down interactions simple: tap card -> open filtered detail screen.

Recommended mobile order:

1. greeting / title
2. financial snapshot
3. quick actions
4. savings progress
5. loan summary
6. insights

---

## 10. Data Strategy For V1

### Recommended V1 query pattern

Use the existing `dashboard` query for the main screen load.
It now supports optional `fromDate`, `toDate`, and `activityLimit` arguments.

Use supporting queries only when the user drills into a section:
- savings detail -> `savingsGoals`
- loans detail -> `loans(type?, status?)`
- repayment detail -> `loanRepaymentHistory(loanId)`

This keeps the initial dashboard fast and easy to reason about.

### Example main query

```graphql
query {
  dashboard(activityLimit: 5) {
    totalIncome
    totalExpenses
    netBalance
    period {
      isFiltered
    }
    insights {
      expenseToIncomeRatio
      hasOverdueLoans
    }
    savingsOverview {
      totalGoals
      completedGoals
      focusGoal {
        name
        remainingAmount
        progress
      }
    }
    savingsProgress {
      id
      name
      targetAmount
      currentAmount
      remainingAmount
      progress
    }
    loanSummary {
      totalOwed
      totalGiven
      activeLoanCount
      overdueLoanCount
      settledLoanCount
    }
    recentActivity {
      type
      title
      subtitle
      amount
      happenedAt
    }
  }
}
```

---

## 11. Empty, Loading, And Error States

### Empty state
When the user has no financial data yet:
- show a friendly onboarding version of the dashboard
- explain what each section becomes after data is added
- prioritize first actions:
  - add income
  - add expense
  - create savings goal

### Partial data state
If only some areas have data:
- still show the dashboard
- render empty modules gracefully
- example:
  - savings exists, loans empty
  - show “No loans yet” instead of hiding the section entirely

### Loading state
- use skeletons for cards, not spinners everywhere
- keep top summary loading separately if needed

### Error state
- show retry affordance
- avoid blocking the whole screen if one secondary section fails
- if possible, preserve previously loaded data while retrying

---

## 12. Metrics And Calculations To Display

### Must-have
- total income
- total expenses
- net balance
- savings progress per goal
- remaining amount per goal
- total owed
- total given

### Nice-to-have from existing data
- savings goals sorted by highest progress or nearest completion
- number of active loans
- number of overdue loans
- total remaining balance across all owed loans

### Later analytics
- monthly trend comparisons
- category spending summaries
- savings velocity
- repayment consistency

---

## 13. Suggested Innovation Layer

Innovation should improve usefulness, not add noise.

Strong candidates:
- contextual insight cards generated from real financial state
- action prompts like “Add today’s expense” or “Record a repayment”
- goal milestone celebrations
- due/overdue loan attention banner
- lightweight personalization such as “focus goal” pinning

Avoid in V1:
- overly complex charts
- too many colors without meaning
- novelty interactions that slow down core tasks

---

## 14. Gaps To Consider Before UI Implementation

The current backend is strong for a useful V1 dashboard, but these may be useful later:

- time-range dashboard filters such as weekly / monthly / yearly
- recent activity feed
- category-level expense summary for charts
- dashboard-specific loan counts by status
- savings goal prioritization logic
- notification and reminder support

These are not blockers for starting dashboard design now.

---

## 15. Phased Delivery Plan

### Phase 1 — Core Dashboard
- financial snapshot
- savings progress cards
- loan summary
- quick actions
- clean empty/loading/error states

### Phase 2 — Richer Loan And Savings Views
- overdue / active / settled loan sections
- repayment-history entry points
- smarter savings prioritization

### Phase 3 — Insights And Trends
- monthly comparisons
- spending patterns
- recommendation cards

---

## 16. Acceptance Criteria For Dashboard Design

The dashboard design is ready when:

- the screen has a clear hierarchy from top summary to deeper sections
- a first-time user can understand what each module means
- savings and loan modules expose both current state and next action
- mobile layout is usable without horizontal compression
- empty, loading, and error states are defined
- every visible metric maps to an existing backend field or a clearly identified future enhancement

---

## 17. Recommended Next Deliverables

After this plan, the next artifacts should be:

1. dashboard wireframe structure
2. component inventory
3. mobile interaction flow
4. visual direction board
5. implementation breakdown for the client app

---

## 18. Final Recommendation

Yes, the project is ready to begin dashboard design now.

The best approach is:
- start with the current `dashboard` query as the source of truth
- design mobile-first
- focus v1 on clarity, actionability, and savings/loan visibility
- add innovation through helpful insights and smart actions, not complexity
