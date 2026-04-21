using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Repositories;

public sealed class SavingsGoalRepository(AppDbContext db) : ISavingsGoalRepository
{
    public Task<List<SavingsGoal>> GetAllAsync(Guid userId, CancellationToken ct = default) =>
        db.SavingsGoals
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
        db.SavingsGoals.Update(goal);
        await db.SaveChangesAsync(ct);
    }
}
