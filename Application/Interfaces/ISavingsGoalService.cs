using Vela.Application.DTOs.Savings;
using Vela.Domain.Common;
using Vela.Domain.Entities;

namespace Vela.Application.Interfaces;

public interface ISavingsGoalService
{
    Task<AppResult<List<SavingsGoal>>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<AppResult<SavingsGoal>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<AppResult<SavingsGoal>> CreateAsync(CreateSavingsGoalInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult<SavingsGoal>> UpdateAsync(UpdateSavingsGoalInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult<SavingsGoal>> AddContributionAsync(AddSavingsContributionInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult<SavingsGoal>> UpdateProgressAsync(Guid id, decimal newAmount, Guid userId, CancellationToken ct = default);
    Task<AppResult> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
