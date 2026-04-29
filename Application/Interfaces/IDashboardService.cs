using FinanceTracker.Application.DTOs.Dashboard;
using FinanceTracker.Domain.Common;

namespace FinanceTracker.Application.Interfaces;

public interface IDashboardService
{
    Task<AppResult<DashboardSummary>> GetSummaryAsync(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        int? activityLimit,
        CancellationToken ct = default);
}
