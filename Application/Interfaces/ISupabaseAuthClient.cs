using Vela.Application.DTOs.Auth;
using Vela.Domain.Common;

namespace Vela.Application.Interfaces;

public interface ISupabaseAuthClient
{
    Task<AppResult<SignUpPayload>> SignUpAsync(string email, string password, CancellationToken ct = default);
    Task<AppResult<SignInPayload>> SignInAsync(string email, string password, CancellationToken ct = default);
}
