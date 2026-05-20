using Vela.Api.Functions.Support;
using Vela.Application.DTOs.Income;
using Vela.Application.Interfaces;
using Vela.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Net;

namespace Vela.Api.Functions;

public sealed class IncomeFunctions(
    IIncomeService incomeService,
    FunctionAuth auth,
    IIdempotencyService idempotencyService)
{
    [Function("GetIncomes")]
    [OpenApiOperation(operationId: "GetIncomes", tags: [ApiDocumentation.IncomeTag], Summary = "Lists income records.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.FromDate, In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Summary = "Start date filter.")]
    [OpenApiParameter(ApiDocumentation.ToDate, In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Summary = "End date filter.")]
    [OpenApiParameter("source", In = ParameterLocation.Query, Required = false, Type = typeof(string), Summary = "Income source filter.")]
    [OpenApiParameter(ApiDocumentation.Limit, In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Maximum number of records to return.")]
    [OpenApiParameter(ApiDocumentation.Offset, In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Number of records to skip.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(Income[]), Description = "Income records.")]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Access token is missing or invalid.")]
    public async Task<IActionResult> GetAllAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Incomes)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var query = FunctionQuery.Parse(req);
        if (!FunctionQuery.TryOptionalDate(query, "fromDate", out var fromDate, out var error) ||
            !FunctionQuery.TryOptionalDate(query, "toDate", out var toDate, out error) ||
            !FunctionQuery.TryOptionalPagination(query, out var pagination, out error))
        {
            return FunctionHttpResults.Validation(req, error);
        }

        var source = FunctionQuery.OptionalString(query, "source");
        var result = await incomeService.GetAllAsync(user.Value, ct);
        if (result.IsFailure) return FunctionHttpResults.FromResult(req, result);

        var items = result.Value!
            .Where(x => (!fromDate.HasValue || x.Date >= fromDate.Value) &&
                        (!toDate.HasValue || x.Date <= toDate.Value))
            .Where(x => string.IsNullOrWhiteSpace(source) ||
                        string.Equals(x.Source, source.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        return FunctionHttpResults.OkList(req, items, pagination);
    }

    [Function("GetIncome")]
    [OpenApiOperation(operationId: "GetIncome", tags: [ApiDocumentation.IncomeTag], Summary = "Gets one income record.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Income ID.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(Income), Description = "Income record.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Income record was not found.")]
    public async Task<IActionResult> GetByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.IncomeById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(req, user.Error)
            : FunctionHttpResults.FromResult(req, await incomeService.GetByIdAsync(id, user.Value, ct));
    }

    [Function("AddIncome")]
    [OpenApiOperation(operationId: "AddIncome", tags: [ApiDocumentation.IncomeTag], Summary = "Creates an income record.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.IdempotencyKey, In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Optional idempotency key for safe retries.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(AddIncomeInput), Required = true, Description = "Income details.")]
    [OpenApiResponseWithBody(HttpStatusCode.Created, ApiDocumentation.Json, typeof(Income), Description = "Income record created.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Request body is invalid.")]
    public async Task<IActionResult> AddAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.Incomes)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var input = await FunctionJson.ReadJsonAsync<AddIncomeInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation(req, "Request body is required.");

        return await FunctionIdempotency.RunAsync(
            req,
            user.Value,
            idempotencyService,
            () => incomeService.AddAsync(input, user.Value, ct),
            StatusCodes.Status201Created,
            ct);
    }

    [Function("UpdateIncome")]
    [OpenApiOperation(operationId: "UpdateIncome", tags: [ApiDocumentation.IncomeTag], Summary = "Updates an income record.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Income ID.")]
    [OpenApiParameter(ApiDocumentation.IdempotencyKey, In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Optional idempotency key for safe retries.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(UpdateIncomeInput), Required = true, Description = "Income details.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(Income), Description = "Income record updated.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Income record was not found.")]
    public async Task<IActionResult> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = ApiRoutes.IncomeById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var input = await FunctionJson.ReadJsonAsync<UpdateIncomeInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation(req, "Request body is required.");

        return await FunctionIdempotency.RunAsync(
            req,
            user.Value,
            idempotencyService,
            () => incomeService.UpdateAsync(input with { Id = id }, user.Value, ct),
            StatusCodes.Status200OK,
            ct);
    }

    [Function("DeleteIncome")]
    [OpenApiOperation(operationId: "DeleteIncome", tags: [ApiDocumentation.IncomeTag], Summary = "Deletes an income record.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Income ID.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Income record deleted.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Income record was not found.")]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = ApiRoutes.IncomeById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(req, user.Error)
            : FunctionHttpResults.Deleted(req, await incomeService.DeleteAsync(id, user.Value, ct));
    }
}
