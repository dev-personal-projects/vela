using FinanceTracker.Api.Functions.Support;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace FinanceTracker.Api.Functions;

public sealed class DashboardFunction(IDashboardService dashboardService, FunctionAuth auth)
{
    [Function("GetDashboard")]
    public async Task<IActionResult> GetAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Dashboard)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(user.Error);

        var query = FunctionQuery.Parse(req);
        if (!FunctionQuery.TryOptionalDate(query, "fromDate", out var fromDate, out var error) ||
            !FunctionQuery.TryOptionalDate(query, "toDate", out var toDate, out error) ||
            !FunctionQuery.TryOptionalInt(query, "activityLimit", out var activityLimit, out error))
        {
            return FunctionHttpResults.Validation(error);
        }

        return FunctionHttpResults.FromResult(
            await dashboardService.GetSummaryAsync(user.Value, fromDate, toDate, activityLimit, ct));
    }
}
