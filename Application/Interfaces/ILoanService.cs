using FinanceTracker.Application.DTOs.Loan;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Application.Interfaces;

public interface ILoanService
{
    Task<AppResult<List<Loan>>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<AppResult<List<Loan>>> GetAllAsync(Guid userId, LoanType? type, LoanStatus? status, CancellationToken ct = default);
    Task<AppResult<Loan>> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<AppResult<List<LoanRepayment>>> GetRepaymentHistoryAsync(Guid loanId, Guid userId, CancellationToken ct = default);
    Task<AppResult<Loan>> AddAsync(AddLoanInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult<Loan>> UpdateAsync(UpdateLoanInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult<Loan>> RecordRepaymentAsync(RecordRepaymentInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult<Loan>> SettleAsync(SettleLoanInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
