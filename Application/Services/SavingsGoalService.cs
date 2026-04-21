using FinanceTracker.Application.DTOs.Savings;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Services;

public sealed class SavingsGoalService(ISavingsGoalRepository repository) : ISavingsGoalService
{
    public async Task<AppResult<List<SavingsGoal>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var goals = await repository.GetAllAsync(userId, ct);
        return AppResult<List<SavingsGoal>>.Ok(goals);
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

        return AppResult<SavingsGoal>.Ok(goal);
    }
}
