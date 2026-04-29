using FinanceTracker.Application.DTOs.Dashboard;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Interfaces;

public interface IDashboardRepository
{
    Task<decimal> GetTotalIncomeAsync(Guid userId, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);
    Task<decimal> GetTotalExpensesAsync(Guid userId, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);
    Task<List<SavingsGoal>> GetSavingsGoalsAsync(Guid userId, CancellationToken ct = default);
    Task<DashboardLoanSummarySnapshot> GetLoanSummaryAsync(Guid userId, CancellationToken ct = default);
    Task<List<DashboardActivityItem>> GetRecentActivityAsync(Guid userId, DateTime? fromDate, DateTime? toDate, int limit, CancellationToken ct = default);
}
