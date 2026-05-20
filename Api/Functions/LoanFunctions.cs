using Vela.Api.Functions.Support;
using Vela.Application.DTOs.Loan;
using Vela.Application.Interfaces;
using Vela.Domain.Entities;
using Vela.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Net;

namespace Vela.Api.Functions;

public sealed class LoanFunctions(
    ILoanService loanService,
    FunctionAuth auth,
    IIdempotencyService idempotencyService)
{
    [Function("GetLoans")]
    [OpenApiOperation(operationId: "GetLoans", tags: [ApiDocumentation.LoanTag], Summary = "Lists loans.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter("type", In = ParameterLocation.Query, Required = false, Type = typeof(LoanType), Summary = "Loan type filter.")]
    [OpenApiParameter("status", In = ParameterLocation.Query, Required = false, Type = typeof(LoanStatus), Summary = "Loan status filter.")]
    [OpenApiParameter(ApiDocumentation.Limit, In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Maximum number of records to return.")]
    [OpenApiParameter(ApiDocumentation.Offset, In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Number of records to skip.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(Loan[]), Description = "Loans.")]
    [OpenApiResponseWithBody(HttpStatusCode.Unauthorized, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Access token is missing or invalid.")]
    public async Task<IActionResult> GetAllAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.Loans)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var query = FunctionQuery.Parse(req);
        if (!FunctionQuery.TryOptionalEnum<LoanType>(query, "type", out var type, out var error) ||
            !FunctionQuery.TryOptionalEnum<LoanStatus>(query, "status", out var status, out error) ||
            !FunctionQuery.TryOptionalPagination(query, out var pagination, out error))
        {
            return FunctionHttpResults.Validation(req, error);
        }

        var result = await loanService.GetAllAsync(user.Value, type, status, ct);
        return result.IsSuccess
            ? FunctionHttpResults.OkList(req, result.Value!, pagination)
            : FunctionHttpResults.FromResult(req, result);
    }

    [Function("GetLoan")]
    [OpenApiOperation(operationId: "GetLoan", tags: [ApiDocumentation.LoanTag], Summary = "Gets one loan.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Loan ID.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(Loan), Description = "Loan.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Loan was not found.")]
    public async Task<IActionResult> GetByIdAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.LoanById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(req, user.Error)
            : FunctionHttpResults.FromResult(req, await loanService.GetByIdAsync(id, user.Value, ct));
    }

    [Function("GetLoanRepayments")]
    [OpenApiOperation(operationId: "GetLoanRepayments", tags: [ApiDocumentation.LoanTag], Summary = "Lists loan repayments.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Loan ID.")]
    [OpenApiParameter(ApiDocumentation.Limit, In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Maximum number of records to return.")]
    [OpenApiParameter(ApiDocumentation.Offset, In = ParameterLocation.Query, Required = false, Type = typeof(int), Summary = "Number of records to skip.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(LoanRepayment[]), Description = "Loan repayments.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Loan was not found.")]
    public async Task<IActionResult> GetRepaymentsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ApiRoutes.LoanRepayments)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var query = FunctionQuery.Parse(req);
        if (!FunctionQuery.TryOptionalPagination(query, out var pagination, out var error))
        {
            return FunctionHttpResults.Validation(req, error);
        }

        var result = await loanService.GetRepaymentHistoryAsync(id, user.Value, ct);
        return result.IsSuccess
            ? FunctionHttpResults.OkList(req, result.Value!, pagination)
            : FunctionHttpResults.FromResult(req, result);
    }

    [Function("AddLoan")]
    [OpenApiOperation(operationId: "AddLoan", tags: [ApiDocumentation.LoanTag], Summary = "Creates a loan.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.IdempotencyKey, In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Optional idempotency key for safe retries.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(AddLoanInput), Required = true, Description = "Loan details.")]
    [OpenApiResponseWithBody(HttpStatusCode.Created, ApiDocumentation.Json, typeof(Loan), Description = "Loan created.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Request body is invalid.")]
    public async Task<IActionResult> AddAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.Loans)]
        HttpRequestData req,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var input = await FunctionJson.ReadJsonAsync<AddLoanInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation(req, "Request body is required.");

        return await FunctionIdempotency.RunAsync(
            req,
            user.Value,
            idempotencyService,
            () => loanService.AddAsync(input, user.Value, ct),
            StatusCodes.Status201Created,
            ct);
    }

    [Function("UpdateLoan")]
    [OpenApiOperation(operationId: "UpdateLoan", tags: [ApiDocumentation.LoanTag], Summary = "Updates a loan.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Loan ID.")]
    [OpenApiParameter(ApiDocumentation.IdempotencyKey, In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Optional idempotency key for safe retries.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(UpdateLoanInput), Required = true, Description = "Loan details.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(Loan), Description = "Loan updated.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Loan was not found.")]
    public async Task<IActionResult> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = ApiRoutes.LoanById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var input = await FunctionJson.ReadJsonAsync<UpdateLoanInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation(req, "Request body is required.");

        return await FunctionIdempotency.RunAsync(
            req,
            user.Value,
            idempotencyService,
            () => loanService.UpdateAsync(input with { Id = id }, user.Value, ct),
            StatusCodes.Status200OK,
            ct);
    }

    [Function("RecordLoanRepayment")]
    [OpenApiOperation(operationId: "RecordLoanRepayment", tags: [ApiDocumentation.LoanTag], Summary = "Records a loan repayment.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Loan ID.")]
    [OpenApiParameter(ApiDocumentation.IdempotencyKey, In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Optional idempotency key for safe retries.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(RecordRepaymentInput), Required = true, Description = "Repayment details.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(Loan), Description = "Loan updated.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Request body is invalid.")]
    public async Task<IActionResult> RecordRepaymentAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.LoanRepayments)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var input = await FunctionJson.ReadJsonAsync<RecordRepaymentInput>(req, ct);
        if (input is null) return FunctionHttpResults.Validation(req, "Request body is required.");

        return await FunctionIdempotency.RunAsync(
            req,
            user.Value,
            idempotencyService,
            () => loanService.RecordRepaymentAsync(input with { LoanId = id }, user.Value, ct),
            StatusCodes.Status200OK,
            ct);
    }

    [Function("SettleLoan")]
    [OpenApiOperation(operationId: "SettleLoan", tags: [ApiDocumentation.LoanTag], Summary = "Settles a loan.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Loan ID.")]
    [OpenApiParameter(ApiDocumentation.IdempotencyKey, In = ParameterLocation.Header, Required = false, Type = typeof(string), Summary = "Optional idempotency key for safe retries.")]
    [OpenApiRequestBody(ApiDocumentation.Json, typeof(SettleLoanInput), Required = false, Description = "Optional settlement details.")]
    [OpenApiResponseWithBody(HttpStatusCode.OK, ApiDocumentation.Json, typeof(Loan), Description = "Loan settled.")]
    [OpenApiResponseWithBody(HttpStatusCode.BadRequest, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Request body is invalid.")]
    public async Task<IActionResult> SettleAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = ApiRoutes.LoanSettle)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        if (user.IsFailure) return FunctionHttpResults.Unauthorized(req, user.Error);

        var input = await FunctionJson.ReadJsonAsync<SettleLoanInput>(req, ct) ?? new SettleLoanInput(id);
        return await FunctionIdempotency.RunAsync(
            req,
            user.Value,
            idempotencyService,
            () => loanService.SettleAsync(input with { LoanId = id }, user.Value, ct),
            StatusCodes.Status200OK,
            ct);
    }

    [Function("DeleteLoan")]
    [OpenApiOperation(operationId: "DeleteLoan", tags: [ApiDocumentation.LoanTag], Summary = "Deletes a loan.")]
    [OpenApiSecurity(ApiDocumentation.BearerScheme, SecuritySchemeType.ApiKey, Name = "Authorization", In = OpenApiSecurityLocationType.Header, Description = "JWT Authorization header using the Bearer scheme.")]
    [OpenApiParameter(ApiDocumentation.Id, In = ParameterLocation.Path, Required = true, Type = typeof(Guid), Summary = "Loan ID.")]
    [OpenApiResponseWithoutBody(HttpStatusCode.NoContent, Description = "Loan deleted.")]
    [OpenApiResponseWithBody(HttpStatusCode.NotFound, ApiDocumentation.Json, typeof(ApiErrorResponse), Description = "Loan was not found.")]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = ApiRoutes.LoanById)]
        HttpRequestData req,
        Guid id,
        CancellationToken ct)
    {
        var user = auth.GetUserId(req);
        return user.IsFailure
            ? FunctionHttpResults.Unauthorized(req, user.Error)
            : FunctionHttpResults.Deleted(req, await loanService.DeleteAsync(id, user.Value, ct));
    }
}
