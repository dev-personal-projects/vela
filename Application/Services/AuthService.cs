using System.Text.RegularExpressions;
using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Domain.Common;

namespace FinanceTracker.Application.Services;

public sealed class AuthService(ISupabaseAuthClient authClient) : IAuthService
{
    private static readonly Regex _emailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public async Task<AppResult<AuthPayload>> SignUpAsync(SignUpInput input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || !_emailRegex.IsMatch(input.Email))
        {
            return AppResult<AuthPayload>.Fail("A valid email address is required.");
        }

        if (string.IsNullOrWhiteSpace(input.Password) || input.Password.Length < 8)
        {
            return AppResult<AuthPayload>.Fail("Password must be at least 8 characters.");
        }

        return await authClient.SignUpAsync(input.Email.Trim().ToLowerInvariant(), input.Password, ct);
    }

    public async Task<AppResult<AuthPayload>> SignInAsync(SignInInput input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.Email) || !_emailRegex.IsMatch(input.Email))
        {
            return AppResult<AuthPayload>.Fail("A valid email address is required.");
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            return AppResult<AuthPayload>.Fail("Password is required.");
        }

        return await authClient.SignInAsync(input.Email.Trim().ToLowerInvariant(), input.Password, ct);
    }
}
