using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Interfaces;

public interface IExpenseRepository
{
    Task<List<Expense>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<Expense?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task AddAsync(Expense expense, CancellationToken ct = default);
    Task UpdateAsync(Expense expense, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
