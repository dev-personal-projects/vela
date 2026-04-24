using FinanceTracker.Application.DTOs.Income;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Interfaces;

public interface IIncomeService
{
    Task<AppResult<List<Income>>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<AppResult<Income>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<AppResult<Income>> AddAsync(AddIncomeInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult<Income>> UpdateAsync(UpdateIncomeInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
