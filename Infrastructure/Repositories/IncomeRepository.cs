using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Repositories;

public sealed class IncomeRepository(AppDbContext db) : IIncomeRepository
{
    public Task<List<Income>> GetAllAsync(Guid userId, CancellationToken ct = default) =>
        db.Incomes
          .Where(x => x.UserId == userId)
          .OrderByDescending(x => x.Date)
          .ToListAsync(ct);

    public Task<Income?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default) =>
        db.Incomes
          .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

    public async Task AddAsync(Income income, CancellationToken ct = default)
    {
        await db.Incomes.AddAsync(income, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var income = await GetByIdAsync(id, userId, ct);

        if (income is not null)
        {
            db.Incomes.Remove(income);
            await db.SaveChangesAsync(ct);
        }
    }
}
