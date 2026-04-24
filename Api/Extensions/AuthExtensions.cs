using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FinanceTracker.Api.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddApiAuth(this IServiceCollection services, IConfiguration config)
    {
        var jwtSecret = config["Auth:JwtSecret"]
            ?? throw new InvalidOperationException("Auth:JwtSecret is not configured.");

        var keyBytes = GetJwtSecretBytes(jwtSecret);
        var signingKey = new SymmetricSecurityKey(keyBytes);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    TryAllIssuerSigningKeys = true,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
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
