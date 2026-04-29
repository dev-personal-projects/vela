using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Domain.Enums;
using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Infrastructure.Repositories;

public sealed class LoanRepository(AppDbContext db) : ILoanRepository
{
    public Task<List<Loan>> GetAllAsync(Guid userId, CancellationToken ct = default) =>
        GetAllAsync(userId, null, ct);

    public Task<List<Loan>> GetAllAsync(Guid userId, LoanType? type, CancellationToken ct = default) =>
        db.Loans
          .Where(x => x.UserId == userId)
          .Where(x => !type.HasValue || x.Type == type.Value)
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
        if (db.Entry(loan).State == EntityState.Detached)
        {
            db.Loans.Attach(loan);
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var affected = await db.Loans
            .Where(x => x.Id == id && x.UserId == userId)
            .ExecuteDeleteAsync(ct);

        return affected > 0;
    }
}
