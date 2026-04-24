using FinanceTracker.Application.DTOs.Loan;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Services;

public sealed class LoanService(ILoanRepository repository) : ILoanService
{
    public async Task<AppResult<List<Loan>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var loans = await repository.GetAllAsync(userId, ct);
        return AppResult<List<Loan>>.Ok(loans);
    }

    public async Task<AppResult<Loan>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var loan = await repository.GetByIdAsync(id, userId, ct);

        return loan is null
            ? AppResult<Loan>.Fail("Loan not found.")
            : AppResult<Loan>.Ok(loan);
    }

    public async Task<AppResult<Loan>> AddAsync(AddLoanInput input, Guid userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.CounterpartyName))
        {
            return AppResult<Loan>.Fail("Counterparty name is required.");
        }

        if (input.Amount <= 0)
        {
            return AppResult<Loan>.Fail("Amount must be greater than 0.");
        }

        if (input.DueDate.HasValue && input.DueDate.Value <= input.Date)
        {
            return AppResult<Loan>.Fail("Due date must be after the loan date.");
        }

        var loan = new Loan(userId, input.CounterpartyName, input.Amount, input.Type, input.Description, input.Date, input.DueDate);

        await repository.AddAsync(loan, ct);

        return AppResult<Loan>.Ok(loan);
    }

    public async Task<AppResult<Loan>> UpdateAsync(UpdateLoanInput input, Guid userId, CancellationToken ct = default)
    {
        var loan = await repository.GetByIdAsync(input.Id, userId, ct);

        if (loan is null)
        {
            return AppResult<Loan>.Fail("Loan not found.");
        }

        if (string.IsNullOrWhiteSpace(input.CounterpartyName))
        {
            return AppResult<Loan>.Fail("Counterparty name is required.");
        }

        if (input.DueDate.HasValue && input.DueDate.Value <= loan.Date)
        {
            return AppResult<Loan>.Fail("Due date must be after the loan date.");
        }

        loan.Update(input.CounterpartyName, input.Description, input.DueDate);

        await repository.UpdateAsync(loan, ct);

        return AppResult<Loan>.Ok(loan);
    }

    public async Task<AppResult<Loan>> RecordRepaymentAsync(RecordRepaymentInput input, Guid userId, CancellationToken ct = default)
    {
        var loan = await repository.GetByIdAsync(input.LoanId, userId, ct);

        if (loan is null)
        {
            return AppResult<Loan>.Fail("Loan not found.");
        }

        if (input.Amount <= 0)
        {
            return AppResult<Loan>.Fail("Repayment amount must be greater than 0.");
        }

        if (input.Amount > loan.RemainingBalance)
        {
            return AppResult<Loan>.Fail($"Repayment amount exceeds the remaining balance of {loan.RemainingBalance:C}.");
        }

        loan.Repay(input.Amount);

        await repository.UpdateAsync(loan, ct);

        return AppResult<Loan>.Ok(loan);
    }

    public async Task<AppResult> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var loan = await repository.GetByIdAsync(id, userId, ct);

        if (loan is null)
        {
            return AppResult.Fail("Loan not found.");
        }

        await repository.DeleteAsync(id, userId, ct);

        return AppResult.Ok();
    }
}
