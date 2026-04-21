using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Repositories;

public sealed class LoanRepository(AppDbContext db) : ILoanRepository
{
    public Task<List<Loan>> GetAllAsync(Guid userId, CancellationToken ct = default) =>
        db.Loans
          .Where(x => x.UserId == userId)
          .OrderByDescending(x => x.Date)
          .ToListAsync(ct);

    public Task<Loan?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default) =>
        db.Loans
          .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);

    public async Task AddAsync(Loan loan, CancellationToken ct = default)
    {
        await db.Loans.AddAsync(loan, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Loan loan, CancellationToken ct = default)
    {
        db.Loans.Update(loan);
        await db.SaveChangesAsync(ct);
    }
}
