using FinanceTracker.Application.DTOs.Income;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Services;

public sealed class IncomeService(IIncomeRepository repository) : IIncomeService
{
    public async Task<AppResult<List<Income>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var incomes = await repository.GetAllAsync(userId, ct);
        return AppResult<List<Income>>.Ok(incomes);
    }

    public async Task<AppResult<Income>> AddAsync(AddIncomeInput input, Guid userId, CancellationToken ct = default)
    {
        if (input.Amount <= 0)
        {
            return AppResult<Income>.Fail("Amount must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(input.Source))
        {
            return AppResult<Income>.Fail("Source is required.");
        }

        if (input.Date > DateTime.UtcNow)
        {
            return AppResult<Income>.Fail("Date cannot be in the future.");
        }

        var income = new Income(userId, input.Amount, input.Source, input.Description, input.Date);

        await repository.AddAsync(income, ct);

        return AppResult<Income>.Ok(income);
    }
}
