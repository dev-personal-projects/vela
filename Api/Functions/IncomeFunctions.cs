using FinanceTracker.Api.Functions.Support;
using FinanceTracker.Application.DTOs.Income;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace FinanceTracker.Api.Functions;

public sealed class IncomeFunctions(IIncomeService incomeService, FunctionAuth auth)
{
    [Function("GetIncomes")]
    public async Task<IActionResult> GetAllAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Incomes)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(user.Error);

        var query = FunctionQuery.Parse(req);
        if (!FunctionQuery.TryOptionalDate(query, "fromDate", out var fromDate, out var error) ||
            !FunctionQuery.TryOptionalDate(query, "toDate", out var toDate, out error))
        {
            return FunctionHttpResults.Validation(error);
        }

        var source = FunctionQuery.OptionalString(query, "source");
        var result = await incomeService.GetAllAsync(user.Value, ct);
        if (result.IsFailure) return FunctionHttpResults.FromResult(result);

        var items = result.Value!
            .Where(x => (!fromDate.HasValue || x.Date >= fromDate.Value) &&
                        (!toDate.HasValue || x.Date <= toDate.Value))
            .Where(x => string.IsNullOrWhiteSpace(source) ||
                        string.Equals(x.Source, source.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new OkObjectResult(items);
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
            ? FunctionHttpResults.Unauthorized(user.Error)
            : FunctionHttpResults.FromResult(await incomeService.GetByIdAsync(id, user.Value, ct));
    }

    [Function("AddIncome")]
    public async Task<IActionResult> AddAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.Incomes)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(user.Error);

        var input = await FunctionJson.ReadJsonAsync<AddIncomeInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation("Request body is required.");

        var result = await incomeService.AddAsync(input, user.Value, ct);
        return result.IsSuccess
            ? new ObjectResult(result.Value) { StatusCode = 201 }
            : FunctionHttpResults.FromResult(result);
    }

    [Function("UpdateIncome")]
    public async Task<IActionResult> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = ApiRoutes.IncomeById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(user.Error);

        var input = await FunctionJson.ReadJsonAsync<UpdateIncomeInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation("Request body is required.");

        return FunctionHttpResults.FromResult(await incomeService.UpdateAsync(input with { Id = id }, user.Value, ct));
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
            ? FunctionHttpResults.Unauthorized(user.Error)
            : FunctionHttpResults.Deleted(await incomeService.DeleteAsync(id, user.Value, ct));
    }
}
