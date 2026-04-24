using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Application.Interfaces;

public interface ILoanRepository
{
    Task<List<Loan>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<Loan?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task AddAsync(Loan loan, CancellationToken ct = default);
    Task UpdateAsync(Loan loan, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
