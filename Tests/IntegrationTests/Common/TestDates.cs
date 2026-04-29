namespace IntegrationTests.Common;

internal static class TestDates
{
    internal static readonly DateTime UtcNow = new(2026, 04, 29, 12, 00, 00, DateTimeKind.Utc);
    internal static readonly DateTime OneDayAgo = UtcNow.AddDays(-1);
    internal static readonly DateTime ThreeDaysAgo = UtcNow.AddDays(-3);
    internal static readonly DateTime OneWeekAgo = UtcNow.AddDays(-7);
    internal static readonly DateTime Tomorrow = UtcNow.AddDays(1);
    internal static readonly DateTime NextWeek = UtcNow.AddDays(7);
}
