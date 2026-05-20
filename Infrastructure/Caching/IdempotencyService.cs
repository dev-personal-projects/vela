using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vela.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace Vela.Infrastructure.Caching;

public sealed class IdempotencyService(IDistributedCache cache, IConfiguration configuration) : IIdempotencyService
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IdempotencyCacheOptions _options = configuration
        .GetSection("Idempotency")
        .Get<IdempotencyCacheOptions>() ?? new IdempotencyCacheOptions();

    public async Task<IdempotencyReplay?> GetAsync(string scope, string key, CancellationToken ct = default)
    {
        var cached = await cache.GetStringAsync(CacheKey(scope, key), ct);
        return string.IsNullOrWhiteSpace(cached)
            ? null
            : JsonSerializer.Deserialize<IdempotencyReplay>(cached, _jsonOptions);
    }

    public Task StoreAsync(string scope, string key, IdempotencyReplay replay, CancellationToken ct = default) =>
        cache.SetStringAsync(
            CacheKey(scope, key),
            JsonSerializer.Serialize(replay, _jsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_options.TtlHours)
            },
            ct);

    private static string CacheKey(string scope, string key)
    {
        var raw = $"{scope}:{key}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        return $"idempotency:{hash}";
    }
}
