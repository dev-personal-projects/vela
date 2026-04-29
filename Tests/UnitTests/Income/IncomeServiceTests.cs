using FinanceTracker.Application.DTOs.Income;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Application.Services;
using UnitTests.Common;

namespace UnitTests.Income;

public sealed class IncomeServiceTests
{
    private readonly Mock<IIncomeRepository> _repository = new();
    private readonly Mock<IDashboardCacheService> _cache = new();

    [Fact(DisplayName = "UT-INCOME-ADD-001 stores valid income and invalidates dashboard cache")]
    public async Task AddAsync_ShouldPersistIncomeAndInvalidateCache_WhenInputIsValid()
    {
        var service = CreateService();
        var userId = TestUsers.PrimaryUserId;
        var ct = TestContext.Current.CancellationToken;
        FinanceTracker.Domain.Entities.Income? savedIncome = null;

        _repository
            .Setup(x => x.AddAsync(It.IsAny<FinanceTracker.Domain.Entities.Income>(), It.IsAny<CancellationToken>()))
            .Callback<FinanceTracker.Domain.Entities.Income, CancellationToken>((income, _) => savedIncome = income)
            .Returns(Task.CompletedTask);

        _cache
            .Setup(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var input = new AddIncomeInput(5000m, " Salary ", "Monthly salary", TestDates.OneDayAgo);

        var result = await service.AddAsync(input, userId, ct);

        result.IsSuccess.Should().BeTrue();
        savedIncome.Should().NotBeNull();
        savedIncome!.Amount.Should().Be(5000m);
        savedIncome.Source.Should().Be("Salary");
        savedIncome.Description.Should().Be("Monthly salary");
        _cache.Verify(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-INCOME-ADD-002 rejects future-dated income records")]
    public async Task AddAsync_ShouldFail_WhenDateIsInTheFuture()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.AddAsync(
            new AddIncomeInput(5000m, "Salary", "Monthly salary", DateTime.UtcNow.AddMinutes(1)),
            TestUsers.PrimaryUserId,
            ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Date cannot be in the future.");

        _repository.Verify(x => x.AddAsync(It.IsAny<FinanceTracker.Domain.Entities.Income>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-INCOME-DELETE-001 does not invalidate cache when delete misses")]
    public async Task DeleteAsync_ShouldNotInvalidateCache_WhenIncomeIsMissing()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        _repository
            .Setup(x => x.DeleteAsync(Guid.Empty, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await service.DeleteAsync(Guid.Empty, TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Income record not found.");

        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private IncomeService CreateService() => new(_repository.Object, _cache.Object);
}
