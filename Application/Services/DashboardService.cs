using FinanceTracker.Application.DTOs.Dashboard;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Application.Services;

public sealed class DashboardService(
    IDashboardRepository dashboardRepository,
    IDashboardCacheService dashboardCacheService) : IDashboardService
{
    public async Task<AppResult<DashboardSummary>> GetSummaryAsync(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        int? activityLimit,
        CancellationToken ct = default)
    {
        var normalizedActivityLimit = NormalizeActivityLimit(activityLimit);
        var cachedDashboard = await dashboardCacheService.GetAsync(
            userId,
            fromDate,
            toDate,
            normalizedActivityLimit,
            ct);

        if (cachedDashboard is not null)
        {
            return AppResult<DashboardSummary>.Ok(cachedDashboard);
        }

        var totalIncome = await dashboardRepository.GetTotalIncomeAsync(userId, fromDate, toDate, ct);
        var totalExpenses = await dashboardRepository.GetTotalExpensesAsync(userId, fromDate, toDate, ct);
        var savingsGoals = await dashboardRepository.GetSavingsGoalsAsync(userId, ct);
        var loanSummary = await dashboardRepository.GetLoanSummaryAsync(userId, ct);
        var recentActivity = await dashboardRepository.GetRecentActivityAsync(
            userId,
            fromDate,
            toDate,
            normalizedActivityLimit,
            ct);
        var netBalance = totalIncome - totalExpenses;

        var savingsProgress = savingsGoals
            .Select(MapSavingsGoalProgress)
            .ToList();

        var completedGoals = savingsGoals.Count(x => x.RemainingAmount == 0);
        var focusGoal = SelectFocusGoal(savingsGoals);

        var dashboard = new DashboardSummary(
            TotalIncome: totalIncome,
            TotalExpenses: totalExpenses,
            NetBalance: netBalance,
            Period: new DashboardPeriodSummary(
                FromDate: fromDate,
                ToDate: toDate,
                IsFiltered: fromDate.HasValue || toDate.HasValue),
            Insights: new DashboardInsights(
                ExpenseToIncomeRatio: CalculateExpenseToIncomeRatio(totalIncome, totalExpenses),
                HasNegativeNetBalance: netBalance < 0,
                HasOverdueLoans: loanSummary.OverdueLoanCount > 0),
            SavingsOverview: new DashboardSavingsOverview(
                TotalGoals: savingsGoals.Count,
                CompletedGoals: completedGoals,
                FocusGoal: focusGoal is null ? null : MapSavingsGoalProgress(focusGoal)),
            SavingsProgress: savingsProgress,
            LoanSummary: new LoanSummary(
                TotalOwed: loanSummary.TotalOwed,
                TotalGiven: loanSummary.TotalGiven,
                ActiveLoanCount: loanSummary.ActiveLoanCount,
                OverdueLoanCount: loanSummary.OverdueLoanCount,
                SettledLoanCount: loanSummary.SettledLoanCount),
            RecentActivity: recentActivity);

        await dashboardCacheService.SetAsync(
            userId,
            fromDate,
            toDate,
            normalizedActivityLimit,
            dashboard,
            ct);

        return AppResult<DashboardSummary>.Ok(dashboard);
    }

    private static decimal? CalculateExpenseToIncomeRatio(decimal totalIncome, decimal totalExpenses) =>
        totalIncome <= 0 ? null : Math.Round(totalExpenses / totalIncome * 100, 2);

    private static int NormalizeActivityLimit(int? activityLimit) =>
        activityLimit switch
        {
            null => 10,
            <= 0 => 10,
            > 25 => 25,
            _ => activityLimit.Value
        };

    private static SavingsGoalProgress MapSavingsGoalProgress(SavingsGoal goal) =>
        new(goal.Id, goal.Name, goal.TargetAmount, goal.CurrentAmount, goal.RemainingAmount, goal.Progress);

    private static SavingsGoal? SelectFocusGoal(IEnumerable<SavingsGoal> goals) =>
        goals
            .Where(x => x.RemainingAmount > 0)
            .OrderBy(x => x.Deadline.HasValue ? 0 : 1)
            .ThenBy(x => x.Deadline)
            .ThenByDescending(x => x.Progress)
            .ThenBy(x => x.RemainingAmount)
            .FirstOrDefault();
}
