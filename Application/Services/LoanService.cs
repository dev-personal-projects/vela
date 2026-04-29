using FinanceTracker.Application.DTOs.Loan;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Application.Services;

public sealed class LoanService(
    ILoanRepository repository,
    ILoanRepaymentRepository repaymentRepository,
    IDashboardCacheService dashboardCacheService) : ILoanService
{
    public async Task<AppResult<List<Loan>>> GetAllAsync(Guid userId, CancellationToken ct = default)
    {
        var loans = await repository.GetAllAsync(userId, ct);
        return AppResult<List<Loan>>.Ok(loans);
    }

    public async Task<AppResult<List<Loan>>> GetAllAsync(Guid userId, LoanType? type, LoanStatus? status, CancellationToken ct = default)
    {
        var loans = await repository.GetAllAsync(userId, type, ct);

        if (status.HasValue)
        {
            loans = loans.Where(x => x.Status == status.Value).ToList();
        }

        return AppResult<List<Loan>>.Ok(loans);
    }

    public async Task<AppResult<Loan>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var loan = await repository.GetByIdAsync(id, userId, ct);

        return loan is null
            ? AppResult<Loan>.Fail("Loan not found.")
            : AppResult<Loan>.Ok(loan);
    }

    public async Task<AppResult<List<LoanRepayment>>> GetRepaymentHistoryAsync(Guid loanId, Guid userId, CancellationToken ct = default)
    {
        var loan = await repository.GetByIdAsync(loanId, userId, ct);

        if (loan is null)
        {
            return AppResult<List<LoanRepayment>>.Fail("Loan not found.");
        }

        var repayments = await repaymentRepository.GetByLoanIdAsync(loanId, ct);
        return AppResult<List<LoanRepayment>>.Ok(repayments);
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

        var loan = new Loan(userId, input.CounterpartyName, input.CounterpartyPhoneNumber, input.Amount, input.Type, input.Description, input.Date, input.DueDate);

        await repository.AddAsync(loan, ct);
        await dashboardCacheService.InvalidateUserAsync(userId, ct);

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

        if (input.Amount <= 0)
        {
            return AppResult<Loan>.Fail("Amount must be greater than 0.");
        }

        if (input.Amount < loan.AmountRepaid)
        {
            return AppResult<Loan>.Fail($"Amount cannot be less than the amount already repaid ({loan.AmountRepaid:C}).");
        }

        if (input.DueDate.HasValue && input.DueDate.Value <= loan.Date)
        {
            return AppResult<Loan>.Fail("Due date must be after the loan date.");
        }

        loan.Update(input.CounterpartyName, input.CounterpartyPhoneNumber, input.Amount, input.Description, input.DueDate);

        await repository.UpdateAsync(loan, ct);
        await dashboardCacheService.InvalidateUserAsync(userId, ct);

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

        var repaymentDate = input.Date ?? DateTime.UtcNow;

        if (repaymentDate < loan.Date)
        {
            return AppResult<Loan>.Fail("Repayment date cannot be before the loan date.");
        }

        loan.Repay(input.Amount);
        var repayment = new LoanRepayment(loan.Id, input.Amount, repaymentDate, input.Note);

        await repaymentRepository.AddAsync(repayment, ct);
        await dashboardCacheService.InvalidateUserAsync(userId, ct);

        return AppResult<Loan>.Ok(loan);
    }

    public async Task<AppResult<Loan>> SettleAsync(SettleLoanInput input, Guid userId, CancellationToken ct = default)
    {
        var loan = await repository.GetByIdAsync(input.LoanId, userId, ct);

        if (loan is null)
        {
            return AppResult<Loan>.Fail("Loan not found.");
        }

        if (loan.IsSettled)
        {
            return AppResult<Loan>.Fail("Loan is already settled.");
        }

        var settlementAmount = loan.RemainingBalance;
        var settlementDate = input.Date ?? DateTime.UtcNow;

        if (settlementDate < loan.Date)
        {
            return AppResult<Loan>.Fail("Repayment date cannot be before the loan date.");
        }

        loan.Repay(settlementAmount);
        var repayment = new LoanRepayment(loan.Id, settlementAmount, settlementDate, input.Note);

        await repaymentRepository.AddAsync(repayment, ct);
        await dashboardCacheService.InvalidateUserAsync(userId, ct);

        return AppResult<Loan>.Ok(loan);
    }

    public async Task<AppResult> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var deleted = await repository.DeleteAsync(id, userId, ct);

        if (deleted)
        {
            await dashboardCacheService.InvalidateUserAsync(userId, ct);
        }

        return deleted ? AppResult.Ok() : AppResult.Fail("Loan not found.");
    }
}
