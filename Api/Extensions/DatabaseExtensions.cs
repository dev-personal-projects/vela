using FinanceTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinanceTracker.Api.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(NormalizePostgresConnectionString(connectionString)));

        return services;
    }

    private static string NormalizePostgresConnectionString(string connectionString)
    {
        var trimmed = connectionString.Trim();

        if (trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException("ConnectionStrings:Default is not a valid PostgreSQL URI.");
            }

            // Example: postgresql://user:pass@host:6543/db?sslmode=require
            var (username, password) = ParseUserInfo(uri.UserInfo);

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort ? 5432 : uri.Port,
                Database = uri.AbsolutePath.TrimStart('/'),
                Username = username,
                Password = password
            };

            var query = ParseQuery(uri.Query);

            if (query.TryGetValue("sslmode", out var sslmode) &&
                TryParseSslMode(sslmode, out var parsedSslMode))
            {
                builder.SslMode = parsedSslMode;
            }

            return builder.ConnectionString;
        }

        return trimmed;
    }

    private static (string Username, string Password) ParseUserInfo(string userInfo)
    {
        if (string.IsNullOrWhiteSpace(userInfo))
        {
            return (string.Empty, string.Empty);
        }

        var parts = userInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(parts[0]);
        var password = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

        return (username, password);
    }

    private static Dictionary<string, string> ParseQuery(string queryString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(queryString))
        {
            return result;
        }

        var query = queryString.StartsWith('?') ? queryString[1..] : queryString;
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(kv[0]);
            var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;

            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static bool TryParseSslMode(string value, out SslMode mode)
    {
        var normalized = value.Trim().ToLowerInvariant().Replace('_', '-');

        mode = normalized switch
        {
            "disable"     => SslMode.Disable,
            "allow"       => SslMode.Allow,
            "prefer"      => SslMode.Prefer,
            "require"     => SslMode.Require,
            "verify-ca"   => SslMode.VerifyCA,
            "verify-full" => SslMode.VerifyFull,
            _             => default
        };

        return normalized is "disable" or "allow" or "prefer" or "require" or "verify-ca" or "verify-full";
    }
}
