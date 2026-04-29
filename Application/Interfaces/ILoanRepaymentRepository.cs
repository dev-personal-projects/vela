using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Interfaces;

public interface ILoanRepaymentRepository
{
    Task<List<LoanRepayment>> GetByLoanIdAsync(Guid loanId, CancellationToken ct = default);
    Task AddAsync(LoanRepayment repayment, CancellationToken ct = default);
}
