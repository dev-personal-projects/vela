using Vela.Api.Functions.Support;
using Vela.Application.DTOs.Income;
using Vela.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Vela.Api.Functions;

public sealed class IncomeFunctions(
    IIncomeService incomeService,
    FunctionAuth auth,
    IIdempotencyService idempotencyService)
{
    [Function("GetIncomes")]
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
