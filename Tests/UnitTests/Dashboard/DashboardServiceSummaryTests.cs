using FinanceTracker.Application.DTOs.Dashboard;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Application.Services;
using UnitTests.Builders;
using UnitTests.Common;

namespace UnitTests.Dashboard;

public sealed class DashboardServiceSummaryTests
{
    [Fact(DisplayName = "UT-DASHBOARD-SUMMARY-001 caps activity limit at 25 before querying recent activity")]
    [Trait("Suite", "Unit")]
    [Trait("Feature", "Dashboard")]
    [Trait("CaseId", "UT-DASHBOARD-SUMMARY-001")]
    public async Task UT_DASHBOARD_SUMMARY_001_CapsActivityLimitAt25_BeforeQueryingRecentActivity()
    {
        var ct = TestContext.Current.CancellationToken;
        var repository = CreateSummaryRepository();
        var cache = new Mock<IDashboardCacheService>(MockBehavior.Strict);
        cache.Setup(x => x.GetAsync(TestUsers.PrimaryUserId, null, null, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DashboardSummary?)null);
        cache.Setup(x => x.SetAsync(TestUsers.PrimaryUserId, null, null, 25, It.IsAny<DashboardSummary>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new DashboardService(repository.Object, cache.Object);

        var result = await service.GetSummaryAsync(TestUsers.PrimaryUserId, null, null, 999, ct);

        result.IsSuccess.Should().BeTrue();
        repository.Verify(x => x.GetRecentActivityAsync(TestUsers.PrimaryUserId, null, null, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-DASHBOARD-SUMMARY-002 composes summary values and selects the correct focus goal")]
    [Trait("Suite", "Unit")]
    [Trait("Feature", "Dashboard")]
    [Trait("CaseId", "UT-DASHBOARD-SUMMARY-002")]
    public async Task UT_DASHBOARD_SUMMARY_002_ComposesSummaryValuesAndSelectsTheCorrectFocusGoal()
    {
        var ct = TestContext.Current.CancellationToken;
        var nearDeadlineGoal = new SavingsGoalBuilder()
            .WithName("Emergency Fund")
            .WithDeadline(TestDates.NextWeek)
            .WithTargetAmount(10_000m)
            .WithCurrentAmount(3_000m)
            .Build();
        var noDeadlineGoal = new SavingsGoalBuilder()
            .WithName("Vacation")
            .WithDeadline(null)
            .WithTargetAmount(5_000m)
            .WithCurrentAmount(4_000m)
            .Build();

        var repository = new Mock<IDashboardRepository>(MockBehavior.Strict);
        var cache = new Mock<IDashboardCacheService>(MockBehavior.Strict);
        cache.Setup(x => x.GetAsync(TestUsers.PrimaryUserId, null, null, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DashboardSummary?)null);
        repository.Setup(x => x.GetTotalIncomeAsync(TestUsers.PrimaryUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15_500m);
        repository.Setup(x => x.GetTotalExpensesAsync(TestUsers.PrimaryUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(150m);
        repository.Setup(x => x.GetSavingsGoalsAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([nearDeadlineGoal, noDeadlineGoal]);
        repository.Setup(x => x.GetLoanSummaryAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardLoanSummarySnapshot(300m, 0m, 0, 1, 0));
        repository.Setup(x => x.GetRecentActivityAsync(TestUsers.PrimaryUserId, null, null, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        cache.Setup(x => x.SetAsync(TestUsers.PrimaryUserId, null, null, 10, It.IsAny<DashboardSummary>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new DashboardService(repository.Object, cache.Object);

        var result = await service.GetSummaryAsync(TestUsers.PrimaryUserId, null, null, null, ct);

        result.IsSuccess.Should().BeTrue();
        result.Value!.NetBalance.Should().Be(15_350m);
        result.Value.Insights.ExpenseToIncomeRatio.Should().Be(0.97m);
        result.Value.Insights.HasOverdueLoans.Should().BeTrue();
        result.Value.SavingsOverview.TotalGoals.Should().Be(2);
        result.Value.SavingsOverview.FocusGoal!.Name.Should().Be("Emergency Fund");
    }

    private static Mock<IDashboardRepository> CreateSummaryRepository()
    {
        var repository = new Mock<IDashboardRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetTotalIncomeAsync(TestUsers.PrimaryUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1_000m);
        repository.Setup(x => x.GetTotalExpensesAsync(TestUsers.PrimaryUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100m);
        repository.Setup(x => x.GetSavingsGoalsAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        repository.Setup(x => x.GetLoanSummaryAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DashboardLoanSummarySnapshot(0m, 0m, 0, 0, 0));
        repository.Setup(x => x.GetRecentActivityAsync(TestUsers.PrimaryUserId, null, null, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        return repository;
    }
}
