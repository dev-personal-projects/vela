using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Application.Interfaces;

public interface ILoanRepository
{
    Task<List<Loan>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<List<Loan>> GetAllAsync(Guid userId, LoanType? type, CancellationToken ct = default);
    Task<Loan?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task AddAsync(Loan loan, CancellationToken ct = default);
    Task UpdateAsync(Loan loan, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
