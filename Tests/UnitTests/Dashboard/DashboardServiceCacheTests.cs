using Vela.Application.DTOs.Dashboard;
using Vela.Application.Interfaces;
using Vela.Application.Services;
using Vela.Domain.Entities;
using UnitTests.Builders;
using UnitTests.Common;

namespace UnitTests.Dashboard;

public sealed class DashboardServiceCacheTests
{
    [Fact(DisplayName = "UT-DASHBOARD-CACHE-001 returns cached dashboard and skips repository calls")]
    [Trait("Suite", "Unit")]
    [Trait("Feature", "Dashboard")]
    [Trait("CaseId", "UT-DASHBOARD-CACHE-001")]
    public async Task UT_DASHBOARD_CACHE_001_ReturnsCachedDashboardAndSkipsRepositoryCalls()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = new Mock<IDashboardRepository>(MockBehavior.Strict);
        var cache = new Mock<IDashboardCacheService>(MockBehavior.Strict);
        var cachedDashboard = new DashboardSummaryBuilder().Build();

        cache.Setup(x => x.GetAsync(TestUsers.PrimaryUserId, null, null, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDashboard);

        var service = new DashboardService(repository.Object, cache.Object);

        var result = await service.GetSummaryAsync(TestUsers.PrimaryUserId, null, null, null, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(cachedDashboard);
        repository.VerifyNoOtherCalls();
        cache.Verify(x => x.SetAsync(It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<DashboardSummary>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-DASHBOARD-CACHE-002 rebuilds dashboard and stores it when cache misses")]
    [Trait("Suite", "Unit")]
    [Trait("Feature", "Dashboard")]
    [Trait("CaseId", "UT-DASHBOARD-CACHE-002")]
    public async Task UT_DASHBOARD_CACHE_002_RebuildsDashboardAndStoresIt_WhenCacheMisses()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = new Mock<IDashboardRepository>(MockBehavior.Strict);
        var cache = new Mock<IDashboardCacheService>(MockBehavior.Strict);
        var goals = new List<SavingsGoal>
        {
            new SavingsGoalBuilder().WithTargetAmount(2_000m).WithCurrentAmount(1_500m).Build()
        };
        var recentActivity = new List<DashboardActivityItem>
        {
            new(Guid.NewGuid(), DashboardActivityType.Income, "Salary", "Monthly salary", 5_000m, TestDates.OneDayAgo)
        };

        cache.Setup(x => x.GetAsync(TestUsers.PrimaryUserId, null, null, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DashboardSummary?)null);
        repository.Setup(x => x.GetTotalIncomeAsync(TestUsers.PrimaryUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5_000m);
        repository.Setup(x => x.GetTotalExpensesAsync(TestUsers.PrimaryUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(120m);
        repository.Setup(x => x.GetSavingsGoalsAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goals);
        repository.Setup(x => x.GetLoanSummaryAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardLoanSummarySnapshot(100m, 0m, 1, 0, 0));
        repository.Setup(x => x.GetRecentActivityAsync(TestUsers.PrimaryUserId, null, null, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentActivity);
        cache.Setup(x => x.SetAsync(TestUsers.PrimaryUserId, null, null, 10, It.IsAny<DashboardSummary>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new DashboardService(repository.Object, cache.Object);

        var result = await service.GetSummaryAsync(TestUsers.PrimaryUserId, null, null, null, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NetBalance.Should().Be(4_880m);
        cache.Verify(x => x.SetAsync(TestUsers.PrimaryUserId, null, null, 10, It.Is<DashboardSummary>(d => d.NetBalance == 4_880m), It.IsAny<CancellationToken>()), Times.Once);
    }
}
