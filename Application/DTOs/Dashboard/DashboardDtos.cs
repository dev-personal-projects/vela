namespace Vela.Application.DTOs.Dashboard;

public sealed record DashboardSummary(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetBalance,
    DashboardPeriodSummary Period,
    DashboardInsights Insights,
    DashboardSavingsOverview SavingsOverview,
    IReadOnlyList<SavingsGoalProgress> SavingsProgress,
    LoanSummary LoanSummary,
    IReadOnlyList<DashboardActivityItem> RecentActivity);

public sealed record DashboardPeriodSummary(
    DateTime? FromDate,
    DateTime? ToDate,
    bool IsFiltered);

public sealed record DashboardInsights(
    decimal? ExpenseToIncomeRatio,
    bool HasNegativeNetBalance,
    bool HasOverdueLoans);

public sealed record DashboardSavingsOverview(
    int TotalGoals,
    int CompletedGoals,
    SavingsGoalProgress? FocusGoal);

public enum DashboardActivityType
{
    Income,
    Expense,
    SavingsGoal,
    Loan,
    LoanRepayment
}

public sealed record DashboardActivityItem(
    Guid ReferenceId,
    DashboardActivityType Type,
    string Title,
    string? Subtitle,
    decimal? Amount,
    DateTime HappenedAt);

public sealed record SavingsGoalProgress(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal RemainingAmount,
    decimal Progress);

public sealed record LoanSummary(
    decimal TotalOwed,
    decimal TotalGiven,
    int ActiveLoanCount,
    int OverdueLoanCount,
    int SettledLoanCount);

public sealed record DashboardLoanSummarySnapshot(
    decimal TotalOwed,
    decimal TotalGiven,
    int ActiveLoanCount,
    int OverdueLoanCount,
    int SettledLoanCount);
