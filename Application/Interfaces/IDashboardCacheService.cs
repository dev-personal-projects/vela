using Vela.Application.DTOs.Dashboard;

namespace Vela.Application.Interfaces;

public interface IDashboardCacheService
{
    Task<DashboardSummary?> GetAsync(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        int activityLimit,
        CancellationToken ct = default);

    Task SetAsync(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        int activityLimit,
        DashboardSummary summary,
        CancellationToken ct = default);

    Task InvalidateUserAsync(Guid userId, CancellationToken ct = default);
}
