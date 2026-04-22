using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Domain.Common;

namespace FinanceTracker.Application.Interfaces;

public interface IAuthService
{
    Task<AppResult<SignUpPayload>> SignUpAsync(SignUpInput input, CancellationToken ct = default);
    Task<AppResult<SignInPayload>> SignInAsync(SignInInput input, CancellationToken ct = default);
}
