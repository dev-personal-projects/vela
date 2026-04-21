using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Domain.Common;

namespace FinanceTracker.Application.Interfaces;

public interface ISupabaseAuthClient
{
    Task<AppResult<AuthPayload>> SignUpAsync(string email, string password, CancellationToken ct = default);
    Task<AppResult<AuthPayload>> SignInAsync(string email, string password, CancellationToken ct = default);
}
