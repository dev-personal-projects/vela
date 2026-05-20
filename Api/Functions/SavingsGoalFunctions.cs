using Vela.Api.Functions.Support;
using Vela.Application.DTOs.Savings;
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

public sealed class SavingsGoalFunctions(
    ISavingsGoalService savingsGoalService,
    FunctionAuth auth,
    IIdempotencyService idempotencyService)
{
    private sealed record UpdateProgressRequest(decimal Amount);

    [Function("GetSavingsGoals")]
    [OpenApiOperation(operationId: "GetSavingsGoals", tags: [ApiDocumentation.SavingsTag], Summary = "Lists savings goals.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Limit, In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Maximum number of records to return.")]
    [OpenApiParameter(ApiDocumentation.Offset, In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Number of records to skip.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(SavingsGoal[]), Description = "Savings goals.")]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Access token is missing or invalid.")]
    public async Task<IActionResult> GetAllAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.SavingsGoals)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var query = FunctionQuery.Parse(req);
        if (!FunctionQuery.TryOptionalPagination(query, out var pagination, out var error))
        {
            return FunctionHttpResults.Validation(req, error);
        }

        var result = await savingsGoalService.GetAllAsync(user.Value, ct);
        return result.IsSuccess
            ? FunctionHttpResults.OkList(req, result.Value!, pagination)
            : FunctionHttpResults.FromResult(req, result);
    }

    [Function("GetSavingsGoal")]
    [OpenApiOperation(operationId: "GetSavingsGoal", tags: [ApiDocumentation.SavingsTag], Summary = "Gets one savings goal.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Savings goal ID.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(SavingsGoal), Description = "Savings goal.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Savings goal was not found.")]
    public async Task<IActionResult> GetByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.SavingsGoalById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(req, user.Error)
            : FunctionHttpResults.FromResult(req, await savingsGoalService.GetByIdAsync(id, user.Value, ct));
    }

    [Function("CreateSavingsGoal")]
    [OpenApiOperation(operationId: "CreateSavingsGoal", tags: [ApiDocumentation.SavingsTag], Summary = "Creates a savings goal.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.IdempotencyKey, In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Optional idempotency key for safe retries.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(CreateSavingsGoalInput), Required = true, Description = "Savings goal details.")]
    [OpenApiResponseWithBody(HttpStatusCode.Created, ApiDocumentation.Json, typeof(SavingsGoal), Description = "Savings goal created.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Request body is invalid.")]
    public async Task<IActionResult> CreateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.SavingsGoals)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var input = await FunctionJson.ReadJsonAsync<CreateSavingsGoalInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation(req, "Request body is required.");

        return await FunctionIdempotency.RunAsync(
            req,
            user.Value,
            idempotencyService,
            () => savingsGoalService.CreateAsync(input, user.Value, ct),
            StatusCodes.Status201Created,
            ct);
    }

    [Function("UpdateSavingsGoal")]
    [OpenApiOperation(operationId: "UpdateSavingsGoal", tags: [ApiDocumentation.SavingsTag], Summary = "Updates a savings goal.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Savings goal ID.")]
    [OpenApiParameter(ApiDocumentation.IdempotencyKey, In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Optional idempotency key for safe retries.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(UpdateSavingsGoalInput), Required = true, Description = "Savings goal details.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(SavingsGoal), Description = "Savings goal updated.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Savings goal was not found.")]
    public async Task<IActionResult> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = ApiRoutes.SavingsGoalById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var input = await FunctionJson.ReadJsonAsync<UpdateSavingsGoalInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation(req, "Request body is required.");

        return await FunctionIdempotency.RunAsync(
            req,
            user.Value,
            idempotencyService,
            () => savingsGoalService.UpdateAsync(input with { Id = id }, user.Value, ct),
            StatusCodes.Status200OK,
            ct);
    }

    [Function("AddSavingsContribution")]
    [OpenApiOperation(operationId: "AddSavingsContribution", tags: [ApiDocumentation.SavingsTag], Summary = "Adds money to a savings goal.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Savings goal ID.")]
    [OpenApiParameter(ApiDocumentation.IdempotencyKey, In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Optional idempotency key for safe retries.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(AddSavingsContributionInput), Required = true, Description = "Contribution details.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(SavingsGoal), Description = "Savings goal updated.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Request body is invalid.")]
    public async Task<IActionResult> AddContributionAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.SavingsGoalContributions)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var input = await FunctionJson.ReadJsonAsync<AddSavingsContributionInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation(req, "Request body is required.");

        return await FunctionIdempotency.RunAsync(
            req,
            user.Value,
            idempotencyService,
            () => savingsGoalService.AddContributionAsync(input with { Id = id }, user.Value, ct),
            StatusCodes.Status200OK,
            ct);
    }

    [Function("UpdateSavingsProgress")]
    [OpenApiOperation(operationId: "UpdateSavingsProgress", tags: [ApiDocumentation.SavingsTag], Summary = "Corrects savings goal progress.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Savings goal ID.")]
    [OpenApiParameter(ApiDocumentation.IdempotencyKey, In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Optional idempotency key for safe retries.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(UpdateProgressRequest), Required = true, Description = "New current amount.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(SavingsGoal), Description = "Savings goal updated.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Request body is invalid.")]
    public async Task<IActionResult> UpdateProgressAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "patch", Route = ApiRoutes.SavingsGoalProgress)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var input = await FunctionJson.ReadJsonAsync<UpdateProgressRequest>(req, ct);
        if (input is null) return FunctionHttpResults.Validation(req, "Request body is required.");

        return await FunctionIdempotency.RunAsync(
            req,
            user.Value,
            idempotencyService,
            () => savingsGoalService.UpdateProgressAsync(id, input.Amount, user.Value, ct),
            StatusCodes.Status200OK,
            ct);
    }

    [Function("DeleteSavingsGoal")]
    [OpenApiOperation(operationId: "DeleteSavingsGoal", tags: [ApiDocumentation.SavingsTag], Summary = "Deletes a savings goal.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Savings goal ID.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Savings goal deleted.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Savings goal was not found.")]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = ApiRoutes.SavingsGoalById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(req, user.Error)
            : FunctionHttpResults.Deleted(req, await savingsGoalService.DeleteAsync(id, user.Value, ct));
    }
}
