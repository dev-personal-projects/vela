using FinanceTracker.Application.DTOs.Expense;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Interfaces;

public interface IExpenseService
{
    Task<AppResult<List<Expense>>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<AppResult<Expense>> AddAsync(AddExpenseInput input, Guid userId, CancellationToken ct = default);
}
