using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Vela.Infrastructure.Auth;

public static class JwtSigningKeyFactory
{
    private const int _minimumSecretBytes = 32;

    public static SymmetricSecurityKey Create(string? jwtSecret)
    {
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            throw new InvalidOperationException("Auth:JwtSecret is not configured.");
        }

        if (LooksLikePlaceholder(jwtSecret))
        {
            throw new InvalidOperationException("Auth:JwtSecret must be supplied from Azure Key Vault, not a placeholder value.");
        }

        var keyBytes = GetJwtSecretBytes(jwtSecret);
        if (keyBytes.Length < _minimumSecretBytes)
        {
            throw new InvalidOperationException($"Auth:JwtSecret must be at least {_minimumSecretBytes} bytes.");
        }

        return new SymmetricSecurityKey(keyBytes);
    }

    private static byte[] GetJwtSecretBytes(string jwtSecret)
    {
        var trimmed = jwtSecret.Trim();

        if (IsProbablyBase64(trimmed))
        {
            try
            {
                var decoded = Convert.FromBase64String(trimmed);
                if (decoded.Length >= _minimumSecretBytes)
                {
                    return decoded;
                }
            }
            catch
            {
                // Fall back to UTF-8 bytes so non-base64 secrets still work.
            }
        }

        return Encoding.UTF8.GetBytes(trimmed);
    }

    private static bool LooksLikePlaceholder(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith('<') && trimmed.EndsWith('>');
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
