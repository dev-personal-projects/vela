using Vela.Domain.Entities;

namespace Vela.Application.Interfaces;

public interface IIncomeRepository
{
    Task<List<Income>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task<Income?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task AddAsync(Income income, CancellationToken ct = default);
    Task UpdateAsync(Income income, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);
}
