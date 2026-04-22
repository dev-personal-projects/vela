using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinanceTracker.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FinanceTracker.Infrastructure.Auth;

public sealed class ApiTokenService : IApiTokenService
{
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly SymmetricSecurityKey _signingKey;
    private readonly TimeSpan _accessTokenLifetime;

    public ApiTokenService(IConfiguration configuration)
    {
        var jwtSecret = configuration["Auth:JwtSecret"]
            ?? throw new InvalidOperationException("Auth:JwtSecret is not configured.");

        _signingKey = new SymmetricSecurityKey(GetJwtSecretBytes(jwtSecret));

        var minutesRaw = configuration["Auth:AccessTokenMinutes"];
        _accessTokenLifetime = int.TryParse(minutesRaw, out var minutes) && minutes > 0
            ? TimeSpan.FromMinutes(minutes)
            : TimeSpan.FromMinutes(60);
    }

    public string CreateAccessToken(Guid userId)
    {
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            claims: claims,
            notBefore: now,
            expires: now.Add(_accessTokenLifetime),
            signingCredentials: new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256));

        return _handler.WriteToken(token);
    }

    private static byte[] GetJwtSecretBytes(string jwtSecret)
    {
        var trimmed = jwtSecret.Trim();

        if (IsProbablyBase64(trimmed))
        {
            try
            {
                var decoded = Convert.FromBase64String(trimmed);
                if (decoded.Length >= 32)
                {
                    return decoded;
                }
            }
            catch
            {
                // fall back to UTF-8 bytes
            }
        }

        return Encoding.UTF8.GetBytes(trimmed);
    }

    private static bool IsProbablyBase64(string value)
    {
        if (value.Length is 0 || value.Length % 4 != 0)
        {
            return false;
        }

        foreach (var ch in value)
        {
            var isBase64Char =
                (ch >= 'A' && ch <= 'Z') ||
                (ch >= 'a' && ch <= 'z') ||
                (ch >= '0' && ch <= '9') ||
                ch is '+' or '/' or '=';

            if (!isBase64Char)
            {
                return false;
            }
        }

        return true;
    }
}

