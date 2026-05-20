using Vela.Application.DTOs.Dashboard;

namespace UnitTests.Builders;

public sealed class DashboardSummaryBuilder
{
    private decimal totalIncome = 5_000m;
    private decimal totalExpenses = 120m;
    private decimal netBalance = 4_880m;
    private DashboardPeriodSummary period = new(null, null, false);
    private DashboardInsights insights = new(2.4m, false, false);
    private DashboardSavingsOverview savingsOverview = new(1, 0, null);
    private IReadOnlyList<SavingsGoalProgress> savingsProgress = [];
    private LoanSummary loanSummary = new(0m, 0m, 0, 0, 0);
    private IReadOnlyList<DashboardActivityItem> recentActivity = [];

    public DashboardSummaryBuilder WithRecentActivity(params DashboardActivityItem[] activity)
    {
        recentActivity = activity;
        return this;
    }

    public DashboardSummaryBuilder WithInsights(DashboardInsights value)
    {
        insights = value;
        return this;
    }

    public DashboardSummaryBuilder WithTotals(decimal income, decimal expenses, decimal balance)
    {
        totalIncome = income;
        totalExpenses = expenses;
        netBalance = balance;
        return this;
    }

    public DashboardSummary Build() =>
        new(
            totalIncome,
            totalExpenses,
            netBalance,
            period,
            insights,
            savingsOverview,
            savingsProgress,
            loanSummary,
            recentActivity);
}
