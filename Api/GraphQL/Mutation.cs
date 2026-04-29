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
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinanceTracker.Api.GraphQL;

public sealed class Mutation
{
    // ── Auth — public ─────────────────────────────────────────────────────────

    public async Task<SignUpPayloadResult> SignUp(
        SignUpInput input,
        [Service] IAuthService service,
        CancellationToken cancellationToken)
    {
        var result = await service.SignUpAsync(input, cancellationToken);

        return result.IsSuccess
            ? new SignUpPayloadResult(result.Value!.Message, null)
            : new SignUpPayloadResult(null, [result.Error!]);
    }

    public async Task<SignInPayloadResult> SignIn(
        SignInInput input,
        [Service] IAuthService service,
        CancellationToken cancellationToken)
    {
        var result = await service.SignInAsync(input, cancellationToken);

        return result.IsSuccess
            ? new SignInPayloadResult(result.Value, null)
            : new SignInPayloadResult(null, [result.Error!]);
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
            ? new IncomePayload(result.Value, null, "Income added successfully.")
            : new IncomePayload(null, [result.Error!], null);
    }

    [Authorize]
    public async Task<IncomePayload> UpdateIncome(
        UpdateIncomeInput input,
        [Service] IIncomeService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        return await ExecuteUpdateAsync(
            () => service.UpdateAsync(input, userId, cancellationToken),
            value => new IncomePayload(value, null, "Income updated successfully."),
            errors => new IncomePayload(null, errors, null),
            "income",
            cancellationToken);
    }

    [Authorize]
    public async Task<DeletePayload> DeleteIncome(
        Guid id,
        [Service] IIncomeService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.DeleteAsync(id, userId, cancellationToken);

        return result.IsSuccess
            ? new DeletePayload(true, null, "Income deleted successfully.")
            : new DeletePayload(false, [result.Error!], null);
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
            ? new ExpensePayload(result.Value, null, "Expense added successfully.")
            : new ExpensePayload(null, [result.Error!], null);
    }

    [Authorize]
    public async Task<ExpensePayload> UpdateExpense(
        UpdateExpenseInput input,
        [Service] IExpenseService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        return await ExecuteUpdateAsync(
            () => service.UpdateAsync(input, userId, cancellationToken),
            value => new ExpensePayload(value, null, "Expense updated successfully."),
            errors => new ExpensePayload(null, errors, null),
            "expense",
            cancellationToken);
    }

    [Authorize]
    public async Task<DeletePayload> DeleteExpense(
        Guid id,
        [Service] IExpenseService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.DeleteAsync(id, userId, cancellationToken);

        return result.IsSuccess
            ? new DeletePayload(true, null, "Expense deleted successfully.")
            : new DeletePayload(false, [result.Error!], null);
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
            ? new SavingsGoalPayload(result.Value, null, "Savings goal created successfully.")
            : new SavingsGoalPayload(null, [result.Error!], null);
    }

    [Authorize]
    public async Task<SavingsGoalPayload> UpdateSavingsGoal(
        UpdateSavingsGoalInput input,
        [Service] ISavingsGoalService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        return await ExecuteUpdateAsync(
            () => service.UpdateAsync(input, userId, cancellationToken),
            value => new SavingsGoalPayload(value, null, "Savings goal updated successfully."),
            errors => new SavingsGoalPayload(null, errors, null),
            "savings goal",
            cancellationToken);
    }

    [Authorize]
    public async Task<SavingsGoalPayload> AddSavingsContribution(
        AddSavingsContributionInput input,
        [Service] ISavingsGoalService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        return await ExecuteUpdateAsync(
            () => service.AddContributionAsync(input, userId, cancellationToken),
            value => new SavingsGoalPayload(value, null, "Savings contribution added successfully."),
            errors => new SavingsGoalPayload(null, errors, null),
            "savings contribution",
            cancellationToken);
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
        return await ExecuteUpdateAsync(
            () => service.UpdateProgressAsync(id, newAmount, userId, cancellationToken),
            value => new SavingsGoalPayload(value, null, "Savings progress updated successfully."),
            errors => new SavingsGoalPayload(null, errors, null),
            "savings progress",
            cancellationToken);
    }

    [Authorize]
    public async Task<DeletePayload> DeleteSavingsGoal(
        Guid id,
        [Service] ISavingsGoalService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.DeleteAsync(id, userId, cancellationToken);

        return result.IsSuccess
            ? new DeletePayload(true, null, "Savings goal deleted successfully.")
            : new DeletePayload(false, [result.Error!], null);
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
            ? new LoanPayload(result.Value, null, "Loan added successfully.")
            : new LoanPayload(null, [result.Error!], null);
    }

    [Authorize]
    public async Task<LoanPayload> UpdateLoan(
        UpdateLoanInput input,
        [Service] ILoanService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        return await ExecuteUpdateAsync(
            () => service.UpdateAsync(input, userId, cancellationToken),
            value => new LoanPayload(value, null, "Loan updated successfully."),
            errors => new LoanPayload(null, errors, null),
            "loan",
            cancellationToken);
    }

    [Authorize]
    public async Task<LoanPayload> RecordRepayment(
        RecordRepaymentInput input,
        [Service] ILoanService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        return await ExecuteUpdateAsync(
            () => service.RecordRepaymentAsync(input, userId, cancellationToken),
            value => new LoanPayload(value, null, "Repayment recorded successfully."),
            errors => new LoanPayload(null, errors, null),
            "loan repayment",
            cancellationToken);
    }

    [Authorize]
    public async Task<LoanPayload> SettleLoan(
        SettleLoanInput input,
        [Service] ILoanService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        return await ExecuteUpdateAsync(
            () => service.SettleAsync(input, userId, cancellationToken),
            value => new LoanPayload(value, null, "Loan settled successfully."),
            errors => new LoanPayload(null, errors, null),
            "loan settlement",
            cancellationToken);
    }

    [Authorize]
    public async Task<DeletePayload> DeleteLoan(
        Guid id,
        [Service] ILoanService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.DeleteAsync(id, userId, cancellationToken);

        return result.IsSuccess
            ? new DeletePayload(true, null, "Loan deleted successfully.")
            : new DeletePayload(false, [result.Error!], null);
    }

    private static async Task<TPayload> ExecuteUpdateAsync<TValue, TPayload>(
        Func<Task<Domain.Common.AppResult<TValue>>> action,
        Func<TValue, TPayload> onSuccess,
        Func<IReadOnlyList<string>, TPayload> onFailure,
        string resourceName,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await action();

            return result.IsSuccess
                ? onSuccess(result.Value!)
                : onFailure([result.Error!]);
        }
        catch (ArgumentException ex)
        {
            return onFailure([ex.Message]);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return onFailure([$"Updating your {resourceName} is taking longer than expected. Refresh to check whether the change was saved before trying again."]);
        }
        catch (TimeoutException)
        {
            return onFailure([$"Updating your {resourceName} timed out. Refresh to verify the latest data before retrying."]);
        }
        catch (DbUpdateException)
        {
            return onFailure([$"We couldn't save your {resourceName} right now. Please try again in a moment."]);
        }
        catch (PostgresException)
        {
            return onFailure([$"We hit a database error while updating your {resourceName}. Please try again shortly."]);
        }
        catch (NpgsqlException)
        {
            return onFailure([$"We couldn't reach the database while updating your {resourceName}. Please try again shortly."]);
        }
        catch
        {
            return onFailure([$"Something went wrong while updating your {resourceName}. Please try again."]);
        }
    }
}
