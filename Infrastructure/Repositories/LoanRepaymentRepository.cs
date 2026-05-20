using Vela.Application.Interfaces;
using Vela.Domain.Entities;
using Vela.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Vela.Infrastructure.Repositories;

public sealed class LoanRepaymentRepository(AppDbContext db) : ILoanRepaymentRepository
{
    public Task<List<LoanRepayment>> GetByLoanIdAsync(Guid loanId, CancellationToken ct = default) =>
        db.LoanRepayments
            .AsNoTracking()
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
