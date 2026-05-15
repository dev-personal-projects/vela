using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Primitives;

namespace FinanceTracker.Api.Functions.Support;

public static class FunctionQuery
{
    public static IDictionary<string, StringValues> Parse(HttpRequestData req) =>
        QueryHelpers.ParseQuery(req.Url.Query);

    public static string? OptionalString(IDictionary<string, StringValues> query, string key)
    {
        if (!query.TryGetValue(key, out var values))
        {
            return null;
        }

        var value = values.FirstOrDefault()?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public static bool TryOptionalDate(
        IDictionary<string, StringValues> query,
        string key,
        out DateTime? value,
        out string? error)
    {
        value = null;
        error = null;

        var raw = OptionalString(query, key);
        if (raw is null)
        {
            return true;
        }

        if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            error = $"Invalid {key}. Expected an ISO-8601 date/time.";
            return false;
        }

        value = parsed.ToUniversalTime();
        return true;
    }

    public static bool TryOptionalInt(
        IDictionary<string, StringValues> query,
        string key,
        out int? value,
        out string? error)
    {
        value = null;
        error = null;

        var raw = OptionalString(query, key);
        if (raw is null)
        {
            return true;
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            error = $"Invalid {key}. Expected integer.";
            return false;
        }

        value = parsed;
        return true;
    }

    public static bool TryOptionalEnum<T>(
        IDictionary<string, StringValues> query,
        string key,
        out T? value,
        out string? error)
        where T : struct, Enum
    {
        value = null;
        error = null;

        var raw = OptionalString(query, key);
        if (raw is null)
        {
            return true;
        }

        if (!Enum.TryParse<T>(raw, ignoreCase: true, out var parsed))
        {
            error = $"Invalid {key}. Expected one of: {string.Join(", ", Enum.GetNames<T>())}.";
            return false;
        }

        value = parsed;
        return true;
    }
}
