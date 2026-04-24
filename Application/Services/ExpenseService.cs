using FinanceTracker.Application.DTOs.Expense;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Services;

public sealed class ExpenseService(IExpenseRepository repository) : IExpenseService
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

        return AppResult<Expense>.Ok(expense);
    }

    public async Task<AppResult> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var expense = await repository.GetByIdAsync(id, userId, ct);

        if (expense is null)
        {
            return AppResult.Fail("Expense record not found.");
        }

        await repository.DeleteAsync(id, userId, ct);

        return AppResult.Ok();
    }
}
