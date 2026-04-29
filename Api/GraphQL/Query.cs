using System.Security.Claims;
using FinanceTracker.Api.GraphQL.Helpers;
using FinanceTracker.Application.DTOs.Dashboard;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using HotChocolate.Authorization;

namespace FinanceTracker.Api.GraphQL;

public sealed class Query
{
    public string Health() => "Finance Tracker API is running!";

    // ── Income ────────────────────────────────────────────────────────────────

    [Authorize]
    public async Task<List<Income>> Incomes(
        DateTime? fromDate,
        DateTime? toDate,
        string? source,
        [Service] IIncomeService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetAllAsync(userId, cancellationToken);

        return result.Value!
            .Where(x => IsWithinDateRange(x.Date, fromDate, toDate))
            .Where(x => MatchesTextFilter(x.Source, source))
            .ToList();
    }

    [Authorize]
    public async Task<Income?> Income(
        Guid id,
        [Service] IIncomeService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetByIdAsync(id, userId, cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }

    // ── Expenses ──────────────────────────────────────────────────────────────

    [Authorize]
    public async Task<List<Expense>> Expenses(
        DateTime? fromDate,
        DateTime? toDate,
        string? category,
        [Service] IExpenseService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetAllAsync(userId, cancellationToken);

        return result.Value!
            .Where(x => IsWithinDateRange(x.Date, fromDate, toDate))
            .Where(x => MatchesTextFilter(x.Category, category))
            .ToList();
    }

    [Authorize]
    public async Task<Expense?> Expense(
        Guid id,
        [Service] IExpenseService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetByIdAsync(id, userId, cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }

    // ── Savings Goals ─────────────────────────────────────────────────────────

    [Authorize]
    public async Task<List<SavingsGoal>> SavingsGoals(
        [Service] ISavingsGoalService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetAllAsync(userId, cancellationToken);
        return result.Value!;
    }

    [Authorize]
    public async Task<SavingsGoal?> SavingsGoal(
        Guid id,
        [Service] ISavingsGoalService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetByIdAsync(id, userId, cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }

    // ── Loans ─────────────────────────────────────────────────────────────────

    [Authorize]
    public async Task<List<Loan>> Loans(
        LoanType? type,
        LoanStatus? status,
        [Service] ILoanService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetAllAsync(userId, type, status, cancellationToken);
        return result.Value!;
    }

    [Authorize]
    public async Task<Loan?> Loan(
        Guid id,
        [Service] ILoanService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetByIdAsync(id, userId, cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }

    [Authorize]
    public async Task<List<LoanRepayment>> LoanRepaymentHistory(
        Guid loanId,
        [Service] ILoanService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetRepaymentHistoryAsync(loanId, userId, cancellationToken);
        return result.IsSuccess ? result.Value! : [];
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [Authorize]
    public async Task<DashboardSummary> Dashboard(
        DateTime? fromDate,
        DateTime? toDate,
        int? activityLimit,
        [Service] IDashboardService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetSummaryAsync(userId, fromDate, toDate, activityLimit, cancellationToken);
        return result.IsSuccess
            ? result.Value!
            : throw new InvalidOperationException(result.Error);
    }

    private static bool IsWithinDateRange(DateTime value, DateTime? fromDate, DateTime? toDate) =>
        (!fromDate.HasValue || value >= fromDate.Value) &&
        (!toDate.HasValue || value <= toDate.Value);

    private static bool MatchesTextFilter(string value, string? filter) =>
        string.IsNullOrWhiteSpace(filter) ||
        string.Equals(value, filter.Trim(), StringComparison.OrdinalIgnoreCase);
}
