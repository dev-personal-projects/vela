using FinanceTracker.Api.Functions.Support;
using FinanceTracker.Application.DTOs.Savings;
using FinanceTracker.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace FinanceTracker.Api.Functions;

public sealed class SavingsGoalFunctions(ISavingsGoalService savingsGoalService, FunctionAuth auth)
{
    private sealed record UpdateProgressRequest(decimal Amount);

    [Function("GetSavingsGoals")]
    public async Task<IActionResult> GetAllAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.SavingsGoals)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(user.Error)
            : FunctionHttpResults.FromResult(await savingsGoalService.GetAllAsync(user.Value, ct));
    }

    [Function("GetSavingsGoal")]
    public async Task<IActionResult> GetByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.SavingsGoalById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(user.Error)
            : FunctionHttpResults.FromResult(await savingsGoalService.GetByIdAsync(id, user.Value, ct));
    }

    [Function("CreateSavingsGoal")]
    public async Task<IActionResult> CreateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.SavingsGoals)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(user.Error);

        var input = await FunctionJson.ReadJsonAsync<CreateSavingsGoalInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation("Request body is required.");

        var result = await savingsGoalService.CreateAsync(input, user.Value, ct);
        return result.IsSuccess
            ? new ObjectResult(result.Value) { StatusCode = 201 }
            : FunctionHttpResults.FromResult(result);
    }

    [Function("UpdateSavingsGoal")]
    public async Task<IActionResult> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = ApiRoutes.SavingsGoalById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(user.Error);

        var input = await FunctionJson.ReadJsonAsync<UpdateSavingsGoalInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation("Request body is required.");

        return FunctionHttpResults.FromResult(await savingsGoalService.UpdateAsync(input with { Id = id }, user.Value, ct));
    }

    [Function("AddSavingsContribution")]
    public async Task<IActionResult> AddContributionAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.SavingsGoalContributions)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(user.Error);

        var input = await FunctionJson.ReadJsonAsync<AddSavingsContributionInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation("Request body is required.");

        return FunctionHttpResults.FromResult(await savingsGoalService.AddContributionAsync(input with { Id = id }, user.Value, ct));
    }

    [Function("UpdateSavingsProgress")]
    public async Task<IActionResult> UpdateProgressAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = ApiRoutes.SavingsGoalProgress)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(user.Error);

        var input = await FunctionJson.ReadJsonAsync<UpdateProgressRequest>(req, ct);
        if (input is null) return FunctionHttpResults.Validation("Request body is required.");

        return FunctionHttpResults.FromResult(await savingsGoalService.UpdateProgressAsync(id, input.Amount, user.Value, ct));
    }

    [Function("DeleteSavingsGoal")]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = ApiRoutes.SavingsGoalById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(user.Error)
            : FunctionHttpResults.Deleted(await savingsGoalService.DeleteAsync(id, user.Value, ct));
    }
}
