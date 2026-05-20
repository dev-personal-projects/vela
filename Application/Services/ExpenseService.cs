using Vela.Application.DTOs.Expense;
using Vela.Application.Interfaces;
using Vela.Domain.Common;
using Vela.Domain.Entities;

namespace Vela.Application.Services;

public sealed class ExpenseService(
    IExpenseRepository repository,
    IDashboardCacheService dashboardCacheService) : IExpenseService
{
    public async Task<AppResult<List<Expense>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var expenses = await repository.GetAllAsync(userId, ct);
        return AppResult<List<Expense>>.Ok(expenses);
    }

    public async Task<AppResult<Expense>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var expense = await repository.GetByIdAsync(id, userId, ct);

        return expense is null
            ? AppResult<Expense>.Fail("Expense record not found.")
            : AppResult<Expense>.Ok(expense);
    }

    public async Task<AppResult<Expense>> AddAsync(AddExpenseInput input, Guid userId, CancellationToken ct = default)
    {
        if (input.Amount <= 0)
        {
            return AppResult<Expense>.Fail("Amount must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(input.Category))
        {
            return AppResult<Expense>.Fail("Category is required.");
        }

        if (input.Date > DateTime.UtcNow)
        {
            return AppResult<Expense>.Fail("Date cannot be in the future.");
        }

        var expense = new Expense(userId, input.Amount, input.Category, input.Description, input.Date);

        await repository.AddAsync(expense, ct);
        await dashboardCacheService.InvalidateUserAsync(userId, ct);

        return AppResult<Expense>.Ok(expense);
    }

    public async Task<AppResult<Expense>> UpdateAsync(UpdateExpenseInput input, Guid userId, CancellationToken ct = default)
    {
        var expense = await repository.GetByIdAsync(input.Id, userId, ct);

        if (expense is null)
        {
            return AppResult<Expense>.Fail("Expense record not found.");
        }

        if (input.Amount <= 0)
        {
            return AppResult<Expense>.Fail("Amount must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(input.Category))
        {
            return AppResult<Expense>.Fail("Category is required.");
        }

        if (input.Date > DateTime.UtcNow)
        {
            return AppResult<Expense>.Fail("Date cannot be in the future.");
        }

        expense.Update(input.Amount, input.Category, input.Description, input.Date);

        await repository.UpdateAsync(expense, ct);
        await dashboardCacheService.InvalidateUserAsync(userId, ct);

        return AppResult<Expense>.Ok(expense);
    }

    public async Task<AppResult> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var deleted = await repository.DeleteAsync(id, userId, ct);

        if (deleted)
        {
            await dashboardCacheService.InvalidateUserAsync(userId, ct);
        }

        return deleted ? AppResult.Ok() : AppResult.Fail("Expense record not found.");
    }
}
