using Vela.Application.DTOs.Auth;
using Vela.Domain.Common;

namespace Vela.Application.Interfaces;

public interface IAuthService
{
    Task<AppResult<SignUpPayload>> SignUpAsync(SignUpInput input, CancellationToken ct = default);
    Task<AppResult<SignInPayload>> SignInAsync(SignInInput input, CancellationToken ct = default);
}
