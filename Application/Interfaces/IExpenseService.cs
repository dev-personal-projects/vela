using FinanceTracker.Application.DTOs.Expense;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Interfaces;

public interface IExpenseService
{
    Task<AppResult<List<Expense>>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<AppResult<Expense>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<AppResult<Expense>> AddAsync(AddExpenseInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult<Expense>> UpdateAsync(UpdateExpenseInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
