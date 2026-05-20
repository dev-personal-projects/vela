using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Vela.Application.Interfaces;
using Vela.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Vela.Infrastructure.Auth;

public sealed class ApiTokenValidator : IApiTokenValidator
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly TokenValidationParameters _validationParameters;

    public ApiTokenValidator(IConfiguration configuration)
    {
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = JwtSigningKeyFactory.Create(configuration["Auth:JwtSecret"]),
            TryAllIssuerSigningKeys = true,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    public AppResult<ClaimsPrincipal> ValidateAccessToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return AppResult<ClaimsPrincipal>.Fail("Authorization header is required.");
        }

        const string bearerPrefix = "Bearer ";
        if (!authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AppResult<ClaimsPrincipal>.Fail("Bearer token is required.");
        }

        var token = authorizationHeader[bearerPrefix.Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AppResult<ClaimsPrincipal>.Fail("Bearer token is required.");
        }

        try
        {
            var principal = _handler.ValidateToken(token, _validationParameters, out _);
            return AppResult<ClaimsPrincipal>.Ok(principal);
        }
        catch (SecurityTokenException)
        {
            return AppResult<ClaimsPrincipal>.Fail("Access token is invalid or expired.");
        }
        catch (ArgumentException)
        {
            return AppResult<ClaimsPrincipal>.Fail("Access token is invalid or expired.");
        }
    }
}
