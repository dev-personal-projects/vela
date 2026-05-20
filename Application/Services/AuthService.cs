using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Vela.Application.DTOs.Auth;
using Vela.Application.Interfaces;
using Vela.Domain.Common;

namespace Vela.Application.Services;

public sealed class AuthService(ISupabaseAuthClient authClient, IApiTokenService apiTokenService) : IAuthService
{
    private static readonly Regex _emailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    public async Task<AppResult<SignUpPayload>> SignUpAsync(SignUpInput input, CancellationToken ct = default)
    {
        var normalizedEmail = input.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedEmail) || !_emailRegex.IsMatch(normalizedEmail))
        {
            return AppResult<SignUpPayload>.Fail("A valid email address is required.");
        }

        if (string.IsNullOrWhiteSpace(input.Password) || input.Password.Length < 8)
        {
            return AppResult<SignUpPayload>.Fail("Password must be at least 8 characters.");
        }

        return await authClient.SignUpAsync(normalizedEmail, input.Password, ct);
    }

    public async Task<AppResult<SignInPayload>> SignInAsync(SignInInput input, CancellationToken ct = default)
    {
        var normalizedEmail = input.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedEmail) || !_emailRegex.IsMatch(normalizedEmail))
        {
            return AppResult<SignInPayload>.Fail("A valid email address is required.");
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            return AppResult<SignInPayload>.Fail("Password is required.");
        }

        var result = await authClient.SignInAsync(normalizedEmail, input.Password, ct);
        if (result.IsFailure)
        {
            return AppResult<SignInPayload>.Fail(result.Error!);
        }

        var supabaseAccessToken = result.Value!.AccessToken;
        var userId = TryGetUserIdFromJwt(supabaseAccessToken);
        if (userId is null)
        {
            return AppResult<SignInPayload>.Fail("Authentication failed — invalid token payload.");
        }

        var apiAccessToken = apiTokenService.CreateAccessToken(userId.Value);

        return AppResult<SignInPayload>.Ok(new SignInPayload(
            apiAccessToken,
            result.Value.RefreshToken,
            result.Value.TokenType));
    }

    private static Guid? TryGetUserIdFromJwt(string token)
    {
        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
            var sub = jwt.Claims.FirstOrDefault(c =>
                c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(sub, out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }
}
