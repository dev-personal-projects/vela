using Vela.Application.Interfaces;
using Vela.Domain.Entities;
using Vela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Vela.Infrastructure.Repositories;

public sealed class IncomeRepository(AppDbContext db) : IIncomeRepository
{
    public Task<List<Income>> GetAllAsync(Guid userId, CancellationToken ct = default) =>
        db.Incomes
          .AsNoTracking()
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

    public async Task UpdateAsync(Income income, CancellationToken ct = default)
    {
        // The entity is usually already tracked (loaded via this same DbContext).
        // Calling Update(...) marks *all* properties as modified and forces a full-row UPDATE.
        if (db.Entry(income).State == EntityState.Detached)
        {
            db.Incomes.Attach(income);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var affected = await db.Incomes
            .Where(x => x.Id == id && x.UserId == userId)
            .ExecuteDeleteAsync(ct);

        return affected > 0;
    }
}
