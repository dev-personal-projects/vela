using Vela.Api.Functions.Support;
using Vela.Application.DTOs.Expense;
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

public sealed class ExpenseFunctions(
    IExpenseService expenseService,
    FunctionAuth auth,
    IIdempotencyService idempotencyService)
{
    [Function("GetExpenses")]
    [OpenApiOperation(operationId: "GetExpenses", tags: [ApiDocumentation.ExpenseTag], Summary = "Lists expense records.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.FromDate, In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Summary = "Start date filter.")]
    [OpenApiParameter(ApiDocumentation.ToDate, In = ParameterLocation.Query, Required = false, Type = typeof(DateTime), Summary = "End date filter.")]
    [OpenApiParameter("category", In = ParameterLocation.Query, Required = false, Type = typeof(string), Summary = "Expense category filter.")]
    [OpenApiParameter(ApiDocumentation.Limit, In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Maximum number of records to return.")]
    [OpenApiParameter(ApiDocumentation.Offset, In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Number of records to skip.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(Expense[]), Description = "Expense records.")]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Access token is missing or invalid.")]
    public async Task<IActionResult> GetAllAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Expenses)]
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

        var category = FunctionQuery.OptionalString(query, "category");
        var result = await expenseService.GetAllAsync(user.Value, ct);
        if (result.IsFailure) return FunctionHttpResults.FromResult(req, result);

        var items = result.Value!
            .Where(x => (!fromDate.HasValue || x.Date >= fromDate.Value) &&
                        (!toDate.HasValue || x.Date <= toDate.Value))
            .Where(x => string.IsNullOrWhiteSpace(category) ||
                        string.Equals(x.Category, category.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        return FunctionHttpResults.OkList(req, items, pagination);
    }

    [Function("GetExpense")]
    [OpenApiOperation(operationId: "GetExpense", tags: [ApiDocumentation.ExpenseTag], Summary = "Gets one expense record.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Expense ID.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(Expense), Description = "Expense record.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Expense record was not found.")]
    public async Task<IActionResult> GetByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.ExpenseById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(req, user.Error)
            : FunctionHttpResults.FromResult(req, await expenseService.GetByIdAsync(id, user.Value, ct));
    }

    [Function("AddExpense")]
    [OpenApiOperation(operationId: "AddExpense", tags: [ApiDocumentation.ExpenseTag], Summary = "Creates an expense record.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.IdempotencyKey, In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Optional idempotency key for safe retries.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(AddExpenseInput), Required = true, Description = "Expense details.")]
    [OpenApiResponseWithBody(HttpStatusCode.Created, ApiDocumentation.Json, typeof(Expense), Description = "Expense record created.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Request body is invalid.")]
    public async Task<IActionResult> AddAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.Expenses)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var input = await FunctionJson.ReadJsonAsync<AddExpenseInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation(req, "Request body is required.");

        return await FunctionIdempotency.RunAsync(
            req,
            user.Value,
            idempotencyService,
            () => expenseService.AddAsync(input, user.Value, ct),
            StatusCodes.Status201Created,
            ct);
    }

    [Function("UpdateExpense")]
    [OpenApiOperation(operationId: "UpdateExpense", tags: [ApiDocumentation.ExpenseTag], Summary = "Updates an expense record.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Expense ID.")]
    [OpenApiParameter(ApiDocumentation.IdempotencyKey, In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Optional idempotency key for safe retries.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(UpdateExpenseInput), Required = true, Description = "Expense details.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(Expense), Description = "Expense record updated.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Expense record was not found.")]
    public async Task<IActionResult> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = ApiRoutes.ExpenseById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var input = await FunctionJson.ReadJsonAsync<UpdateExpenseInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation(req, "Request body is required.");

        return await FunctionIdempotency.RunAsync(
            req,
            user.Value,
            idempotencyService,
            () => expenseService.UpdateAsync(input with { Id = id }, user.Value, ct),
            StatusCodes.Status200OK,
            ct);
    }

    [Function("DeleteExpense")]
    [OpenApiOperation(operationId: "DeleteExpense", tags: [ApiDocumentation.ExpenseTag], Summary = "Deletes an expense record.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Expense ID.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Expense record deleted.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Expense record was not found.")]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = ApiRoutes.ExpenseById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(req, user.Error)
            : FunctionHttpResults.Deleted(req, await expenseService.DeleteAsync(id, user.Value, ct));
    }
}
