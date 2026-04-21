using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Domain.Common;

namespace FinanceTracker.Application.Interfaces;

public interface IAuthService
{
    Task<AppResult<AuthPayload>> SignUpAsync(SignUpInput input, CancellationToken ct = default);
    Task<AppResult<AuthPayload>> SignInAsync(SignInInput input, CancellationToken ct = default);
}
