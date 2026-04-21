using System.Security.Claims;
using FinanceTracker.Api.GraphQL.Helpers;
using FinanceTracker.Api.GraphQL.Payloads;
using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Application.DTOs.Expense;
using FinanceTracker.Application.DTOs.Income;
using FinanceTracker.Application.DTOs.Loan;
using FinanceTracker.Application.DTOs.Savings;
using FinanceTracker.Application.Interfaces;
using HotChocolate.Authorization;

namespace FinanceTracker.Api.GraphQL;

public sealed class Mutation
{
    // ── Auth — public ─────────────────────────────────────────────────────────

    public async Task<AuthPayloadResult> SignUp(
        SignUpInput input,
        [Service] IAuthService service,
        CancellationToken cancellationToken)
    {
        var result = await service.SignUpAsync(input, cancellationToken);

        return result.IsSuccess
            ? new AuthPayloadResult(result.Value, null)
            : new AuthPayloadResult(null, [result.Error!]);
    }

    public async Task<AuthPayloadResult> SignIn(
        SignInInput input,
        [Service] IAuthService service,
        CancellationToken cancellationToken)
    {
        var result = await service.SignInAsync(input, cancellationToken);

        return result.IsSuccess
            ? new AuthPayloadResult(result.Value, null)
            : new AuthPayloadResult(null, [result.Error!]);
    }

    // ── Income ────────────────────────────────────────────────────────────────

    [Authorize]
    public async Task<IncomePayload> AddIncome(
        AddIncomeInput input,
        [Service] IIncomeService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.AddAsync(input, userId, cancellationToken);

        return result.IsSuccess
            ? new IncomePayload(result.Value, null)
            : new IncomePayload(null, [result.Error!]);
    }

    // ── Expenses ──────────────────────────────────────────────────────────────

    [Authorize]
    public async Task<ExpensePayload> AddExpense(
        AddExpenseInput input,
        [Service] IExpenseService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.AddAsync(input, userId, cancellationToken);

        return result.IsSuccess
            ? new ExpensePayload(result.Value, null)
            : new ExpensePayload(null, [result.Error!]);
    }

    // ── Savings Goals ─────────────────────────────────────────────────────────

    [Authorize]
    public async Task<SavingsGoalPayload> CreateSavingsGoal(
        CreateSavingsGoalInput input,
        [Service] ISavingsGoalService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.CreateAsync(input, userId, cancellationToken);

        return result.IsSuccess
            ? new SavingsGoalPayload(result.Value, null)
            : new SavingsGoalPayload(null, [result.Error!]);
    }

    [Authorize]
    public async Task<SavingsGoalPayload> UpdateSavingsProgress(
        Guid id,
        decimal newAmount,
        [Service] ISavingsGoalService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.UpdateProgressAsync(id, newAmount, userId, cancellationToken);

        return result.IsSuccess
            ? new SavingsGoalPayload(result.Value, null)
            : new SavingsGoalPayload(null, [result.Error!]);
    }

    // ── Loans ─────────────────────────────────────────────────────────────────

    [Authorize]
    public async Task<LoanPayload> AddLoan(
        AddLoanInput input,
        [Service] ILoanService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.AddAsync(input, userId, cancellationToken);

        return result.IsSuccess
            ? new LoanPayload(result.Value, null)
            : new LoanPayload(null, [result.Error!]);
    }

    [Authorize]
    public async Task<LoanPayload> RecordRepayment(
        RecordRepaymentInput input,
        [Service] ILoanService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.RecordRepaymentAsync(input, userId, cancellationToken);

        return result.IsSuccess
            ? new LoanPayload(result.Value, null)
            : new LoanPayload(null, [result.Error!]);
    }
}
