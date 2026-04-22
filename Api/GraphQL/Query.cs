using System.Security.Claims;
using FinanceTracker.Api.GraphQL.Helpers;
using FinanceTracker.Api.GraphQL.Payloads;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using HotChocolate.Authorization;

namespace FinanceTracker.Api.GraphQL;

public sealed class Query
{
    // ── Income ────────────────────────────────────────────────────────────────

    [Authorize]
    public async Task<List<Income>> Incomes(
        [Service] IIncomeService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetAllAsync(userId, cancellationToken);
        return result.Value!;
    }

    // ── Expenses ──────────────────────────────────────────────────────────────

    [Authorize]
    public async Task<List<Expense>> Expenses(
        [Service] IExpenseService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetAllAsync(userId, cancellationToken);
        return result.Value!;
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

    // ── Loans ─────────────────────────────────────────────────────────────────

    [Authorize]
    public async Task<List<Loan>> Loans(
        [Service] ILoanService service,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.GetUserId();
        var result = await service.GetAllAsync(userId, cancellationToken);
        return result.Value!;
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
}
