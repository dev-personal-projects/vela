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

    [Fact(DisplayName = "UT-SAVINGS-CREATE-002 rejects empty savings goal names")]
    public async Task CreateAsync_ShouldFail_WhenNameIsMissing()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.CreateAsync(new CreateSavingsGoalInput("   ", 1000m, TestDates.NextMonth), TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Name is required.");

        _repository.Verify(x => x.AddAsync(It.IsAny<SavingsGoal>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-SAVINGS-CREATE-003 rejects non-positive target amounts")]
    public async Task CreateAsync_ShouldFail_WhenTargetAmountIsNotPositive()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.CreateAsync(new CreateSavingsGoalInput("Emergency Fund", 0m, TestDates.NextMonth), TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Target amount must be greater than 0.");

        _repository.Verify(x => x.AddAsync(It.IsAny<SavingsGoal>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
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

    [Fact(DisplayName = "UT-SAVINGS-CONTRIB-002 adds valid contributions and invalidates dashboard cache")]
    public async Task AddContributionAsync_ShouldUpdateGoalAndInvalidateCache_WhenAmountIsValid()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var userId = TestUsers.PrimaryUserId;
        var goal = new SavingsGoalBuilder()
            .WithTargetAmount(1000m)
            .WithCurrentAmount(200m)
            .Build(userId);

        _repository
            .Setup(x => x.GetByIdAsync(goal.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        _repository
            .Setup(x => x.UpdateAsync(goal, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cache
            .Setup(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.AddContributionAsync(new AddSavingsContributionInput(goal.Id, 250m), userId, ct);

        result.IsSuccess.Should().BeTrue();
        goal.CurrentAmount.Should().Be(450m);
        _cache.Verify(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-SAVINGS-CONTRIB-003 rejects non-positive contributions")]
    public async Task AddContributionAsync_ShouldFail_WhenAmountIsNotPositive()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var goal = new SavingsGoalBuilder().Build(TestUsers.PrimaryUserId);

        _repository
            .Setup(x => x.GetByIdAsync(goal.Id, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        var result = await service.AddContributionAsync(new AddSavingsContributionInput(goal.Id, 0m), TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Contribution amount must be greater than 0.");

        _repository.Verify(x => x.UpdateAsync(It.IsAny<SavingsGoal>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-SAVINGS-PROGRESS-001 updates progress and invalidates dashboard cache")]
    public async Task UpdateProgressAsync_ShouldUpdateProgressAndInvalidateCache_WhenAmountIsValid()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var userId = TestUsers.PrimaryUserId;
        var goal = new SavingsGoalBuilder()
            .WithTargetAmount(1000m)
            .Build(userId);

        _repository
            .Setup(x => x.GetByIdAsync(goal.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        _repository
            .Setup(x => x.UpdateAsync(goal, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cache
            .Setup(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.UpdateProgressAsync(goal.Id, 400m, userId, ct);

        result.IsSuccess.Should().BeTrue();
        goal.CurrentAmount.Should().Be(400m);
        goal.Progress.Should().Be(40m);
        _cache.Verify(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-SAVINGS-PROGRESS-002 rejects negative progress amounts")]
    public async Task UpdateProgressAsync_ShouldFail_WhenAmountIsNegative()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var goal = new SavingsGoalBuilder().Build(TestUsers.PrimaryUserId);

        _repository
            .Setup(x => x.GetByIdAsync(goal.Id, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        var result = await service.UpdateProgressAsync(goal.Id, -1m, TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Amount cannot be negative.");

        _repository.Verify(x => x.UpdateAsync(It.IsAny<SavingsGoal>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-SAVINGS-PROGRESS-003 rejects progress amounts above target")]
    public async Task UpdateProgressAsync_ShouldFail_WhenAmountExceedsTarget()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var goal = new SavingsGoalBuilder()
            .WithTargetAmount(1000m)
            .Build(TestUsers.PrimaryUserId);

        _repository
            .Setup(x => x.GetByIdAsync(goal.Id, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(goal);

        var result = await service.UpdateProgressAsync(goal.Id, 1001m, TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Amount cannot exceed the target amount.");

        _repository.Verify(x => x.UpdateAsync(It.IsAny<SavingsGoal>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-SAVINGS-DELETE-001 does not invalidate cache when delete misses")]
    public async Task DeleteAsync_ShouldNotInvalidateCache_WhenGoalIsMissing()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var goalId = Guid.NewGuid();

        _repository
            .Setup(x => x.DeleteAsync(goalId, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await service.DeleteAsync(goalId, TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Savings goal not found.");
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-SAVINGS-DELETE-002 invalidates dashboard cache after a successful delete")]
    public async Task DeleteAsync_ShouldInvalidateCache_WhenDeleteSucceeds()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var goalId = Guid.NewGuid();

        _repository
            .Setup(x => x.DeleteAsync(goalId, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _cache
            .Setup(x => x.InvalidateUserAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.DeleteAsync(goalId, TestUsers.PrimaryUserId, ct);

        result.IsSuccess.Should().BeTrue();
        _cache.Verify(x => x.InvalidateUserAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private SavingsGoalService CreateService() => new(_repository.Object, _cache.Object);
}
