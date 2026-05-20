using Vela.Application.DTOs.Dashboard;
using Vela.Infrastructure.Caching;
using IntegrationTests.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IntegrationTests.Caching;

public sealed class DashboardCacheServiceTests
{
    [Fact(DisplayName = "IT-DASH-CACHE-001 returns null when dashboard cache entry is missing")]
    public async Task GetAsync_ShouldReturnNull_WhenCacheMisses()
    {
        var service = CreateService();

        var result = await service.GetAsync(TestUsers.PrimaryUserId, null, null, 10, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "IT-DASH-CACHE-002 stores and reads dashboard summaries")]
    public async Task SetAsync_ShouldStoreDashboard_ForLaterReads()
    {
        var service = CreateService();
        var summary = CreateSummary(totalIncome: 1000m, totalExpenses: 250m);

        await service.SetAsync(TestUsers.PrimaryUserId, null, null, 10, summary, TestContext.Current.CancellationToken);

        var result = await service.GetAsync(TestUsers.PrimaryUserId, null, null, 10, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.TotalIncome.Should().Be(1000m);
        result.TotalExpenses.Should().Be(250m);
        result.NetBalance.Should().Be(750m);
    }

    [Fact(DisplayName = "IT-DASH-CACHE-003 varies keys by filters user and activity limit")]
    public async Task GetAsync_ShouldReturnOnlyMatchingEntry_WhenInputsDiffer()
    {
        var service = CreateService();
        var fromDate = TestDates.ThreeDaysAgo;
        var toDate = TestDates.UtcNow;

        await service.SetAsync(TestUsers.PrimaryUserId, fromDate, toDate, 10, CreateSummary(totalIncome: 100m), TestContext.Current.CancellationToken);
        await service.SetAsync(TestUsers.SecondaryUserId, fromDate, toDate, 10, CreateSummary(totalIncome: 200m), TestContext.Current.CancellationToken);
        await service.SetAsync(TestUsers.PrimaryUserId, fromDate, toDate, 25, CreateSummary(totalIncome: 300m), TestContext.Current.CancellationToken);

        var primaryResult = await service.GetAsync(TestUsers.PrimaryUserId, fromDate, toDate, 10, TestContext.Current.CancellationToken);
        var secondaryResult = await service.GetAsync(TestUsers.SecondaryUserId, fromDate, toDate, 10, TestContext.Current.CancellationToken);
        var limitResult = await service.GetAsync(TestUsers.PrimaryUserId, fromDate, toDate, 25, TestContext.Current.CancellationToken);
        var missingResult = await service.GetAsync(TestUsers.PrimaryUserId, null, toDate, 10, TestContext.Current.CancellationToken);

        primaryResult!.TotalIncome.Should().Be(100m);
        secondaryResult!.TotalIncome.Should().Be(200m);
        limitResult!.TotalIncome.Should().Be(300m);
        missingResult.Should().BeNull();
    }

    [Fact(DisplayName = "IT-DASH-CACHE-004 invalidation isolates old dashboard entries")]
    public async Task InvalidateUserAsync_ShouldCausePreviousDashboardReadsToMiss()
    {
        var service = CreateService();

        await service.SetAsync(TestUsers.PrimaryUserId, null, null, 10, CreateSummary(totalIncome: 100m), TestContext.Current.CancellationToken);
        await service.InvalidateUserAsync(TestUsers.PrimaryUserId, TestContext.Current.CancellationToken);

        var result = await service.GetAsync(TestUsers.PrimaryUserId, null, null, 10, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "IT-DASH-CACHE-005 disabled cache bypasses reads writes and invalidation")]
    public async Task DisabledCache_ShouldBypassReadsWritesAndInvalidation()
    {
        var service = CreateService(enabled: false);

        await service.SetAsync(TestUsers.PrimaryUserId, null, null, 10, CreateSummary(totalIncome: 100m), TestContext.Current.CancellationToken);
        await service.InvalidateUserAsync(TestUsers.PrimaryUserId, TestContext.Current.CancellationToken);

        var result = await service.GetAsync(TestUsers.PrimaryUserId, null, null, 10, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact(DisplayName = "IT-DASH-CACHE-006 removes unreadable cached payloads safely")]
    public async Task GetAsync_ShouldRemoveCorruptedPayloadAndReturnNull()
    {
        var cache = CreateCache();
        var service = CreateService(cache: cache);
        var cacheKey = BuildDashboardKey(TestUsers.PrimaryUserId, null, null, 10, "v1");

        await cache.SetStringAsync(cacheKey, "{not valid json", TestContext.Current.CancellationToken);

        var result = await service.GetAsync(TestUsers.PrimaryUserId, null, null, 10, TestContext.Current.CancellationToken);
        var remainingPayload = await cache.GetStringAsync(cacheKey, TestContext.Current.CancellationToken);

        result.Should().BeNull();
        remainingPayload.Should().BeNull();
    }

    private static DashboardCacheService CreateService(
        bool enabled = true,
        IDistributedCache? cache = null)
        => new(
            cache ?? CreateCache(),
            Options.Create(new DashboardCacheOptions
            {
                Enabled = enabled,
                TtlSeconds = 60,
                VersionTtlHours = 24
            }),
            NullLogger<DashboardCacheService>.Instance);

    private static MemoryDistributedCache CreateCache() =>
        new(Options.Create(new MemoryDistributedCacheOptions()));

    private static DashboardSummary CreateSummary(decimal totalIncome = 500m, decimal totalExpenses = 100m) =>
        new(
            totalIncome,
            totalExpenses,
            totalIncome - totalExpenses,
            new DashboardPeriodSummary(null, null, false),
            new DashboardInsights(totalIncome <= 0 ? null : totalExpenses / totalIncome * 100, totalIncome < totalExpenses, false),
            new DashboardSavingsOverview(0, 0, null),
            [],
            new LoanSummary(0m, 0m, 0, 0, 0),
            []);

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
