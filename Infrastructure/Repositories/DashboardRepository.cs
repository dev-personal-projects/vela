using FinanceTracker.Application.DTOs.Dashboard;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Repositories;

public sealed class DashboardRepository(AppDbContext db) : IDashboardRepository
{
    public async Task<decimal> GetTotalIncomeAsync(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default) =>
        await db.Incomes
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => !fromDate.HasValue || x.Date >= fromDate.Value)
            .Where(x => !toDate.HasValue || x.Date <= toDate.Value)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;

    public async Task<decimal> GetTotalExpensesAsync(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default) =>
        await db.Expenses
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => !fromDate.HasValue || x.Date >= fromDate.Value)
            .Where(x => !toDate.HasValue || x.Date <= toDate.Value)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0;

    public Task<List<SavingsGoal>> GetSavingsGoalsAsync(Guid userId, CancellationToken ct = default) =>
        db.SavingsGoals
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<DashboardLoanSummarySnapshot> GetLoanSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var utcNow = DateTime.UtcNow;

        var summary = await db.Loans
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .GroupBy(_ => 1)
            .Select(g => new DashboardLoanSummarySnapshot(
                g.Where(x => x.Type == LoanType.Owed).Sum(x => x.Amount - x.AmountRepaid),
                g.Where(x => x.Type == LoanType.Given).Sum(x => x.Amount - x.AmountRepaid),
                g.Count(x => x.AmountRepaid < x.Amount && (!x.DueDate.HasValue || x.DueDate.Value >= utcNow)),
                g.Count(x => x.AmountRepaid < x.Amount && x.DueDate.HasValue && x.DueDate.Value < utcNow),
                g.Count(x => x.AmountRepaid >= x.Amount)))
            .SingleOrDefaultAsync(ct);

        return summary ?? new DashboardLoanSummarySnapshot(0, 0, 0, 0, 0);
    }

    public async Task<List<DashboardActivityItem>> GetRecentActivityAsync(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        int limit,
        CancellationToken ct = default)
    {
        var incomes = await db.Incomes
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => !fromDate.HasValue || x.Date >= fromDate.Value)
            .Where(x => !toDate.HasValue || x.Date <= toDate.Value)
            .OrderByDescending(x => x.Date)
            .Take(limit)
            .Select(x => new DashboardActivityItem(
                x.Id,
                DashboardActivityType.Income,
                x.Source,
                x.Description == string.Empty ? "Income recorded" : x.Description,
                x.Amount,
                x.Date))
            .ToListAsync(ct);

        var expenses = await db.Expenses
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => !fromDate.HasValue || x.Date >= fromDate.Value)
            .Where(x => !toDate.HasValue || x.Date <= toDate.Value)
            .OrderByDescending(x => x.Date)
            .Take(limit)
            .Select(x => new DashboardActivityItem(
                x.Id,
                DashboardActivityType.Expense,
                x.Category,
                x.Description == string.Empty ? "Expense recorded" : x.Description,
                x.Amount,
                x.Date))
            .ToListAsync(ct);

        var savingsGoals = await db.SavingsGoals
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => !fromDate.HasValue || x.CreatedAt >= fromDate.Value)
            .Where(x => !toDate.HasValue || x.CreatedAt <= toDate.Value)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new DashboardActivityItem(
                x.Id,
                DashboardActivityType.SavingsGoal,
                x.Name,
                "Savings goal created",
                x.TargetAmount,
                x.CreatedAt))
            .ToListAsync(ct);

        var loans = await db.Loans
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Where(x => !fromDate.HasValue || x.Date >= fromDate.Value)
            .Where(x => !toDate.HasValue || x.Date <= toDate.Value)
            .OrderByDescending(x => x.Date)
            .Take(limit)
            .Select(x => new DashboardActivityItem(
                x.Id,
                DashboardActivityType.Loan,
                x.CounterpartyName,
                x.Type == LoanType.Owed ? "Loan you owe was added" : "Loan owed to you was added",
                x.Amount,
                x.Date))
            .ToListAsync(ct);

        var recentLoanRepayments = await (
            from repayment in db.LoanRepayments.AsNoTracking()
            join loan in db.Loans.AsNoTracking() on repayment.LoanId equals loan.Id
            where loan.UserId == userId
            where !fromDate.HasValue || repayment.Date >= fromDate.Value
            where !toDate.HasValue || repayment.Date <= toDate.Value
            orderby repayment.Date descending, repayment.CreatedAt descending
            select new DashboardActivityItem(
                repayment.Id,
                DashboardActivityType.LoanRepayment,
                loan.CounterpartyName,
                repayment.Note == string.Empty ? "Repayment recorded" : repayment.Note,
                repayment.Amount,
                repayment.Date))
            .Take(limit)
            .ToListAsync(ct);

        var activity = incomes
            .Concat(expenses)
            .Concat(savingsGoals)
            .Concat(loans)
            .Concat(recentLoanRepayments)
            .OrderByDescending(x => x.HappenedAt)
            .ThenByDescending(x => x.Amount ?? 0)
            .Take(limit)
            .ToList();

        return activity;
    }
}
