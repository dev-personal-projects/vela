using FinanceTracker.Application.DTOs.Loan;
using FinanceTracker.Domain.Common;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Interfaces;

public interface ILoanService
{
    Task<AppResult<List<Loan>>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<AppResult<Loan>> AddAsync(AddLoanInput input, Guid userId, CancellationToken ct = default);
    Task<AppResult<Loan>> RecordRepaymentAsync(RecordRepaymentInput input, Guid userId, CancellationToken ct = default);
}
