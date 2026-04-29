using FinanceTracker.Application.DTOs.Savings;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Application.Services;
using FinanceTracker.Domain.Entities;
using UnitTests.Builders;
using UnitTests.Common;

namespace UnitTests.Savings;

public sealed class SavingsGoalServiceTests
{
    private readonly Mock<ISavingsGoalRepository> _repository = new();
    private readonly Mock<IDashboardCacheService> _cache = new();

    [Fact(DisplayName = "UT-SAVINGS-CREATE-001 stores valid savings goals and invalidates dashboard cache")]
    public async Task CreateAsync_ShouldPersistGoalAndInvalidateCache_WhenInputIsValid()
    {
        var service = CreateService();
        var userId = TestUsers.PrimaryUserId;
        var ct = TestContext.Current.CancellationToken;
        SavingsGoal? savedGoal = null;

        _repository
            .Setup(x => x.AddAsync(It.IsAny<SavingsGoal>(), It.IsAny<CancellationToken>()))
            .Callback<SavingsGoal, CancellationToken>((goal, _) => savedGoal = goal)
            .Returns(Task.CompletedTask);

        _cache
            .Setup(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.CreateAsync(new CreateSavingsGoalInput("Emergency Fund", 10000m, TestDates.NextMonth), userId, ct);

        result.IsSuccess.Should().BeTrue();
        savedGoal.Should().NotBeNull();
        savedGoal!.Name.Should().Be("Emergency Fund");
        savedGoal.TargetAmount.Should().Be(10000m);
        _cache.Verify(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-SAVINGS-UPDATE-001 rejects target amounts below the current saved amount")]
    public async Task UpdateAsync_ShouldFail_WhenTargetAmountIsBelowCurrentAmount()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var goal = new SavingsGoalBuilder()
            .WithTargetAmount(1000m)
            .WithCurrentAmount(600m)
            .Build(TestUsers.PrimaryUserId);

        _repository
            .Setup(x => x.GetByIdAsync(goal.Id, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        var result = await service.UpdateAsync(
            new UpdateSavingsGoalInput(goal.Id, "Emergency Fund", 500m, TestDates.NextMonth),
            TestUsers.PrimaryUserId,
            ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Target amount cannot be less than the current saved amount.");

        _repository.Verify(x => x.UpdateAsync(It.IsAny<SavingsGoal>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-SAVINGS-CONTRIB-001 rejects contributions that would exceed the target amount")]
    public async Task AddContributionAsync_ShouldFail_WhenContributionWouldExceedTarget()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var goal = new SavingsGoalBuilder()
            .WithTargetAmount(1000m)
            .WithCurrentAmount(900m)
            .Build(TestUsers.PrimaryUserId);

        _repository
            .Setup(x => x.GetByIdAsync(goal.Id, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        var result = await service.AddContributionAsync(new AddSavingsContributionInput(goal.Id, 200m), TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Contribution would exceed the target amount.");

        _repository.Verify(x => x.UpdateAsync(It.IsAny<SavingsGoal>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private SavingsGoalService CreateService() => new(_repository.Object, _cache.Object);
}
