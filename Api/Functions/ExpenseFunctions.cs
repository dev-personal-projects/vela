using FinanceTracker.Api.Functions.Support;
using FinanceTracker.Application.DTOs.Expense;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace FinanceTracker.Api.Functions;

public sealed class ExpenseFunctions(IExpenseService expenseService, FunctionAuth auth)
{
    [Function("GetExpenses")]
    public async Task<IActionResult> GetAllAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Expenses)]
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

        var category = FunctionQuery.OptionalString(query, "category");
        var result = await expenseService.GetAllAsync(user.Value, ct);
        if (result.IsFailure) return FunctionHttpResults.FromResult(result);

        var items = result.Value!
            .Where(x => (!fromDate.HasValue || x.Date >= fromDate.Value) &&
                        (!toDate.HasValue || x.Date <= toDate.Value))
            .Where(x => string.IsNullOrWhiteSpace(category) ||
                        string.Equals(x.Category, category.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new OkObjectResult(items);
    }

    [Function("GetExpense")]
    public async Task<IActionResult> GetByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.ExpenseById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(user.Error)
            : FunctionHttpResults.FromResult(await expenseService.GetByIdAsync(id, user.Value, ct));
    }

    [Function("AddExpense")]
    public async Task<IActionResult> AddAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.Expenses)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(user.Error);

        var input = await FunctionJson.ReadJsonAsync<AddExpenseInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation("Request body is required.");

        var result = await expenseService.AddAsync(input, user.Value, ct);
        return result.IsSuccess
            ? new ObjectResult(result.Value) { StatusCode = 201 }
            : FunctionHttpResults.FromResult(result);
    }

    [Function("UpdateExpense")]
    public async Task<IActionResult> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = ApiRoutes.ExpenseById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(user.Error);

        var input = await FunctionJson.ReadJsonAsync<UpdateExpenseInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation("Request body is required.");

        return FunctionHttpResults.FromResult(await expenseService.UpdateAsync(input with { Id = id }, user.Value, ct));
    }

    [Function("DeleteExpense")]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = ApiRoutes.ExpenseById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(user.Error)
            : FunctionHttpResults.Deleted(await expenseService.DeleteAsync(id, user.Value, ct));
    }
}
