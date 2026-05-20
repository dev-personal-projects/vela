using Vela.Api.Functions.Support;
using Vela.Application.DTOs.Loan;
using Vela.Application.Interfaces;
using Vela.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Vela.Api.Functions;

public sealed class LoanFunctions(
    ILoanService loanService,
    FunctionAuth auth,
    IIdempotencyService idempotencyService)
{
    [Function("GetLoans")]
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
