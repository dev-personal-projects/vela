using Vela.Application.Interfaces;
using Vela.Domain.Entities;
using Vela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Vela.Infrastructure.Repositories;

public sealed class ExpenseRepository(AppDbContext db) : IExpenseRepository
{
    public Task<List<Expense>> GetAllAsync(Guid userId, CancellationToken ct = default) =>
        db.Expenses
          .AsNoTracking()
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
        if (db.Entry(expense).State == EntityState.Detached)
        {
            db.Expenses.Attach(expense);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var affected = await db.Expenses
            .Where(x => x.Id == id && x.UserId == userId)
            .ExecuteDeleteAsync(ct);

        return affected > 0;
    }
}
