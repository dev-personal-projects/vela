using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Interfaces;

public interface ISavingsGoalRepository
{
    Task<List<SavingsGoal>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<SavingsGoal?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task AddAsync(SavingsGoal goal, CancellationToken ct = default);
    Task UpdateAsync(SavingsGoal goal, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
