using Microsoft.Azure.Functions.Worker.Http;

namespace Vela.Api.Functions.Support;

public static class FunctionRequest
{
    public const string RequestIdHeader = "X-Request-Id";
    public const string IdempotencyKeyHeader = "Idempotency-Key";

    public static string GetRequestId(HttpRequestData req)
    {
        if (req.Headers.TryGetValues(RequestIdHeader, out var values))
        {
            var requestId = values.FirstOrDefault()?.Trim();
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                return requestId;
            }
        }

        return Guid.NewGuid().ToString("N");
    }

    public static string? GetIdempotencyKey(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues(IdempotencyKeyHeader, out var values))
        {
            return null;
        }

        var key = values.FirstOrDefault()?.Trim();
        return string.IsNullOrWhiteSpace(key) ? null : key;
    }
}
