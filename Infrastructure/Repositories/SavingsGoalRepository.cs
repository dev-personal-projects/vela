using Vela.Application.Interfaces;
using Vela.Domain.Entities;
using Vela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Vela.Infrastructure.Repositories;

public sealed class SavingsGoalRepository(AppDbContext db) : ISavingsGoalRepository
{
    public Task<List<SavingsGoal>> GetAllAsync(Guid userId, CancellationToken ct = default) =>
        db.SavingsGoals
          .AsNoTracking()
          .Where(x => x.UserId == userId)
          .OrderByDescending(x => x.CreatedAt)
          .ToListAsync(ct);

    public Task<SavingsGoal?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default) =>
        db.SavingsGoals
          .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

    public async Task AddAsync(SavingsGoal goal, CancellationToken ct = default)
    {
        await db.SavingsGoals.AddAsync(goal, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(SavingsGoal goal, CancellationToken ct = default)
    {
        if (db.Entry(goal).State == EntityState.Detached)
        {
            db.SavingsGoals.Attach(goal);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var affected = await db.SavingsGoals
            .Where(x => x.Id == id && x.UserId == userId)
            .ExecuteDeleteAsync(ct);

        return affected > 0;
    }
}
