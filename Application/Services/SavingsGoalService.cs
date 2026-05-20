using Vela.Application.DTOs.Savings;
using Vela.Application.Interfaces;
using Vela.Domain.Common;
using Vela.Domain.Entities;

namespace Vela.Application.Services;

public sealed class SavingsGoalService(
    ISavingsGoalRepository repository,
    IDashboardCacheService dashboardCacheService) : ISavingsGoalService
{
    public async Task<AppResult<List<SavingsGoal>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var goals = await repository.GetAllAsync(userId, ct);
        return AppResult<List<SavingsGoal>>.Ok(goals);
    }

    public async Task<AppResult<SavingsGoal>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var goal = await repository.GetByIdAsync(id, userId, ct);

        return goal is null
            ? AppResult<SavingsGoal>.Fail("Savings goal not found.")
            : AppResult<SavingsGoal>.Ok(goal);
    }

    public async Task<AppResult<SavingsGoal>> CreateAsync(CreateSavingsGoalInput input, Guid userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return AppResult<SavingsGoal>.Fail("Name is required.");
        }

        if (input.TargetAmount <= 0)
        {
            return AppResult<SavingsGoal>.Fail("Target amount must be greater than 0.");
        }

        var goal = new SavingsGoal(userId, input.Name, input.TargetAmount, input.Deadline);

        await repository.AddAsync(goal, ct);
        await dashboardCacheService.InvalidateUserAsync(userId, ct);

        return AppResult<SavingsGoal>.Ok(goal);
    }

    public async Task<AppResult<SavingsGoal>> UpdateAsync(UpdateSavingsGoalInput input, Guid userId, CancellationToken ct = default)
    {
        var goal = await repository.GetByIdAsync(input.Id, userId, ct);

        if (goal is null)
        {
            return AppResult<SavingsGoal>.Fail("Savings goal not found.");
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return AppResult<SavingsGoal>.Fail("Name is required.");
        }

        if (input.TargetAmount <= 0)
        {
            return AppResult<SavingsGoal>.Fail("Target amount must be greater than 0.");
        }

        if (input.TargetAmount < goal.CurrentAmount)
        {
            return AppResult<SavingsGoal>.Fail("Target amount cannot be less than the current saved amount.");
        }

        goal.Update(input.Name, input.TargetAmount, input.Deadline);

        await repository.UpdateAsync(goal, ct);
        await dashboardCacheService.InvalidateUserAsync(userId, ct);

        return AppResult<SavingsGoal>.Ok(goal);
    }

    public async Task<AppResult<SavingsGoal>> AddContributionAsync(AddSavingsContributionInput input, Guid userId, CancellationToken ct = default)
    {
        var goal = await repository.GetByIdAsync(input.Id, userId, ct);

        if (goal is null)
        {
            return AppResult<SavingsGoal>.Fail("Savings goal not found.");
        }

        if (input.Amount <= 0)
        {
            return AppResult<SavingsGoal>.Fail("Contribution amount must be greater than 0.");
        }

        if (goal.CurrentAmount + input.Amount > goal.TargetAmount)
        {
            return AppResult<SavingsGoal>.Fail("Contribution would exceed the target amount.");
        }

        goal.AddContribution(input.Amount);

        await repository.UpdateAsync(goal, ct);
        await dashboardCacheService.InvalidateUserAsync(userId, ct);

        return AppResult<SavingsGoal>.Ok(goal);
    }

    public async Task<AppResult<SavingsGoal>> UpdateProgressAsync(Guid id, decimal newAmount, Guid userId, CancellationToken ct = default)
    {
        var goal = await repository.GetByIdAsync(id, userId, ct);

        if (goal is null)
        {
            return AppResult<SavingsGoal>.Fail("Savings goal not found.");
        }

        if (newAmount < 0)
        {
            return AppResult<SavingsGoal>.Fail("Amount cannot be negative.");
        }

        if (newAmount > goal.TargetAmount)
        {
            return AppResult<SavingsGoal>.Fail("Amount cannot exceed the target amount.");
        }

        goal.UpdateProgress(newAmount);

        await repository.UpdateAsync(goal, ct);
        await dashboardCacheService.InvalidateUserAsync(userId, ct);

        return AppResult<SavingsGoal>.Ok(goal);
    }

    public async Task<AppResult> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var deleted = await repository.DeleteAsync(id, userId, ct);

        if (deleted)
        {
            await dashboardCacheService.InvalidateUserAsync(userId, ct);
        }

        return deleted ? AppResult.Ok() : AppResult.Fail("Savings goal not found.");
    }
}
