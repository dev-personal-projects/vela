using Vela.Application.DTOs.Dashboard;
using Vela.Domain.Common;

namespace Vela.Application.Interfaces;

public interface IDashboardService
{
    Task<AppResult<DashboardSummary>> GetSummaryAsync(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        int? activityLimit,
        CancellationToken ct = default);
}
