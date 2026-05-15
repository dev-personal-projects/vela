using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace FinanceTracker.Api.Functions.Support;

public static class FunctionJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T?> ReadJsonAsync<T>(HttpRequestData req, CancellationToken ct)
    {
        if (req.Body is null)
        {
            return default;
        }

        return await JsonSerializer.DeserializeAsync<T>(req.Body, Options, ct);
    }
}
