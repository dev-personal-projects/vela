using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Repositories;

public sealed class ExpenseRepository(AppDbContext db) : IExpenseRepository
{
    public Task<List<Expense>> GetAllAsync(Guid userId, CancellationToken ct = default) =>
        db.Expenses
          .Where(x => x.UserId == userId)
          .OrderByDescending(x => x.Date)
          .ToListAsync(ct);

    public Task<Expense?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default) =>
        db.Expenses
          .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

    public async Task AddAsync(Expense expense, CancellationToken ct = default)
    {
        await db.Expenses.AddAsync(expense, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Expense expense, CancellationToken ct = default)
    {
        db.Expenses.Update(expense);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var expense = await GetByIdAsync(id, userId, ct);

        if (expense is not null)
        {
            db.Expenses.Remove(expense);
            await db.SaveChangesAsync(ct);
        }
    }
}
