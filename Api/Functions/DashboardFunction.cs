using Vela.Api.Functions.Support;
using Vela.Application.DTOs.Dashboard;
using Vela.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Net;

namespace Vela.Api.Functions;

public sealed class DashboardFunction(IDashboardService dashboardService, FunctionAuth auth)
{
    [Function("GetDashboard")]
    [OpenApiOperation(operationId: "GetDashboard", tags: [ApiDocumentation.DashboardTag], Summary = "Gets dashboard summary metrics.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.FromDate, In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Summary = "Start date filter.")]
    [OpenApiParameter(ApiDocumentation.ToDate, In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Summary = "End date filter.")]
    [OpenApiParameter("activityLimit", In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Maximum recent activity items.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(DashboardSummary), Description = "Dashboard summary.")]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Access token is missing or invalid.")]
    public async Task<IActionResult> GetAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Dashboard)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var query = FunctionQuery.Parse(req);
        if (!FunctionQuery.TryOptionalDate(query, "fromDate", out var fromDate, out var error) ||
            !FunctionQuery.TryOptionalDate(query, "toDate", out var toDate, out error) ||
            !FunctionQuery.TryOptionalInt(query, "activityLimit", out var activityLimit, out error))
        {
            return FunctionHttpResults.Validation(req, error);
        }

        return FunctionHttpResults.FromResult(req,
            await dashboardService.GetSummaryAsync(user.Value, fromDate, toDate, activityLimit, ct));
    }
}
