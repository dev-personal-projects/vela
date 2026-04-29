using System.Text.Json;
using FinanceTracker.Application.DTOs.Dashboard;
using FinanceTracker.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace FinanceTracker.Infrastructure.Caching;

public sealed class DashboardCacheService(
    IDistributedCache cache,
    IOptions<DashboardCacheOptions> options,
    ILogger<DashboardCacheService> logger) : IDashboardCacheService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly DashboardCacheOptions _options = options.Value;

    public async Task<DashboardSummary?> GetAsync(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        int activityLimit,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var version = await GetVersionTokenAsync(userId, ct);
        var cacheKey = BuildDashboardKey(userId, fromDate, toDate, activityLimit, version);
        var payload = await cache.GetStringAsync(cacheKey, ct);

        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<DashboardSummary>(payload, _jsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize cached dashboard response for user {UserId}.", userId);
            await cache.RemoveAsync(cacheKey, ct);
            return null;
        }
    }

    public async Task SetAsync(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        int activityLimit,
        DashboardSummary summary,
        CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var version = await GetVersionTokenAsync(userId, ct);
        var cacheKey = BuildDashboardKey(userId, fromDate, toDate, activityLimit, version);
        var payload = JsonSerializer.Serialize(summary, _jsonOptions);

        await cache.SetStringAsync(
            cacheKey,
            payload,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Math.Max(1, _options.TtlSeconds))
            },
            ct);
    }

    public async Task InvalidateUserAsync(Guid userId, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        await cache.SetStringAsync(
            BuildVersionKey(userId),
            Guid.NewGuid().ToString("N"),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(Math.Max(1, _options.VersionTtlHours))
            },
            ct);
    }

    private async Task<string> GetVersionTokenAsync(Guid userId, CancellationToken ct)
    {
        var versionKey = BuildVersionKey(userId);
        var version = await cache.GetStringAsync(versionKey, ct);

        if (!string.IsNullOrWhiteSpace(version))
        {
            return version;
        }

        version = "v1";

        await cache.SetStringAsync(
            versionKey,
            version,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(Math.Max(1, _options.VersionTtlHours))
            },
            ct);

        return version;
    }

    private static string BuildVersionKey(Guid userId) => $"dashboard:version:{userId:N}";

    private static string BuildDashboardKey(
        Guid userId,
        DateTime? fromDate,
        DateTime? toDate,
        int activityLimit,
        string version)
        => string.Join(
            ":",
            "dashboard",
            userId.ToString("N"),
            version,
            FormatDateSegment(fromDate),
            FormatDateSegment(toDate),
            activityLimit);

    private static string FormatDateSegment(DateTime? value) =>
        value.HasValue ? value.Value.ToUniversalTime().ToString("O") : "none";
}
