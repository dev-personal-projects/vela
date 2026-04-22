using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Domain.Common;

namespace FinanceTracker.Application.Interfaces;

public interface ISupabaseAuthClient
{
    Task<AppResult<SignUpPayload>> SignUpAsync(string email, string password, CancellationToken ct = default);
    Task<AppResult<SignInPayload>> SignInAsync(string email, string password, CancellationToken ct = default);
}
