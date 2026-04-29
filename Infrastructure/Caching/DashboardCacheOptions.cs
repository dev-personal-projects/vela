namespace FinanceTracker.Infrastructure.Caching;

public sealed class DashboardCacheOptions
{
    public const string SectionName = "DashboardCache";

    public bool Enabled { get; set; } = true;
    public int TtlSeconds { get; set; } = 60;
    public int VersionTtlHours { get; set; } = 24;
}
