using System.Security.Claims;
using FinanceTracker.Api.GraphQL.Helpers;
using FinanceTracker.Api.GraphQL.Payloads;
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
        [Service] ILoanService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetAllAsync(userId, cancellationToken);

        return result.Value!
            .Where(x => !type.HasValue || x.Type == type.Value)
            .ToList();
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

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [Authorize]
    public async Task<DashboardSummary> Dashboard(
        [Service] IIncomeService incomeService,
        [Service] IExpenseService expenseService,
        [Service] ISavingsGoalService savingsService,
        [Service] ILoanService loanService,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();

        var incomes      = (await incomeService.GetAllAsync(userId, cancellationToken)).Value!;
        var expenses     = (await expenseService.GetAllAsync(userId, cancellationToken)).Value!;
        var savingsGoals = (await savingsService.GetAllAsync(userId, cancellationToken)).Value!;
        var loans        = (await loanService.GetAllAsync(userId, cancellationToken)).Value!;

        var totalIncome   = incomes.Sum(x => x.Amount);
        var totalExpenses = expenses.Sum(x => x.Amount);

        var savingsProgress = savingsGoals
            .Select(g => new SavingsGoalProgress(g.Id, g.Name, g.TargetAmount, g.CurrentAmount, g.Progress))
            .ToList();

        var loanSummary = new LoanSummary(
            TotalOwed:  loans.Where(l => l.Type == Domain.Enums.LoanType.Owed).Sum(l => l.RemainingBalance),
            TotalGiven: loans.Where(l => l.Type == Domain.Enums.LoanType.Given).Sum(l => l.RemainingBalance));

        return new DashboardSummary(
            TotalIncome:     totalIncome,
            TotalExpenses:   totalExpenses,
            NetBalance:      totalIncome - totalExpenses,
            SavingsProgress: savingsProgress,
            LoanSummary:     loanSummary);
    }

    private static bool IsWithinDateRange(DateTime value, DateTime? fromDate, DateTime? toDate) =>
        (!fromDate.HasValue || value >= fromDate.Value) &&
        (!toDate.HasValue || value <= toDate.Value);

    private static bool MatchesTextFilter(string value, string? filter) =>
        string.IsNullOrWhiteSpace(filter) ||
        string.Equals(value, filter.Trim(), StringComparison.OrdinalIgnoreCase);
}
