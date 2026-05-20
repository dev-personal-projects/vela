using Vela.Application.DTOs.Income;
using Vela.Domain.Common;
using Vela.Domain.Entities;

namespace Vela.Application.Interfaces;

public interface IIncomeService
{
    Task<AppResult<List<Income>>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<AppResult<Income>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<AppResult<Income>> AddAsync(AddIncomeInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult<Income>> UpdateAsync(UpdateIncomeInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
