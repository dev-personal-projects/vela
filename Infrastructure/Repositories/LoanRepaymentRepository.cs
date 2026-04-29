using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Repositories;

public sealed class LoanRepaymentRepository(AppDbContext db) : ILoanRepaymentRepository
{
    public Task<List<LoanRepayment>> GetByLoanIdAsync(Guid loanId, CancellationToken ct = default) =>
        db.LoanRepayments
            .Where(x => x.LoanId == loanId)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(LoanRepayment repayment, CancellationToken ct = default)
    {
        await db.LoanRepayments.AddAsync(repayment, ct);
        await db.SaveChangesAsync(ct);
    }
}
