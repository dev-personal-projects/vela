using Vela.Application.DTOs.Income;
using Vela.Application.Interfaces;
using Vela.Application.Services;
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
        Vela.Domain.Entities.Income? savedIncome = null;

        _repository
            .Setup(x => x.AddAsync(It.IsAny<Vela.Domain.Entities.Income>(), It.IsAny<CancellationToken>()))
            .Callback<Vela.Domain.Entities.Income, CancellationToken>((income, _) => savedIncome = income)
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

        _repository.Verify(x => x.AddAsync(It.IsAny<Vela.Domain.Entities.Income>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-INCOME-ADD-003 rejects non-positive income amounts")]
    public async Task AddAsync_ShouldFail_WhenAmountIsNotPositive()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.AddAsync(
            new AddIncomeInput(0m, "Salary", "Monthly salary", TestDates.OneDayAgo),
            TestUsers.PrimaryUserId,
            ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Amount must be greater than 0.");

        _repository.Verify(x => x.AddAsync(It.IsAny<Vela.Domain.Entities.Income>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-INCOME-ADD-004 rejects empty income sources")]
    public async Task AddAsync_ShouldFail_WhenSourceIsMissing()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.AddAsync(
            new AddIncomeInput(100m, "   ", "Monthly salary", TestDates.OneDayAgo),
            TestUsers.PrimaryUserId,
            ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Source is required.");

        _repository.Verify(x => x.AddAsync(It.IsAny<Vela.Domain.Entities.Income>(), It.IsAny<CancellationToken>()), Times.Never);
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

    [Fact(DisplayName = "UT-INCOME-DELETE-002 invalidates dashboard cache after a successful delete")]
    public async Task DeleteAsync_ShouldInvalidateCache_WhenDeleteSucceeds()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var incomeId = Guid.NewGuid();

        _repository
            .Setup(x => x.DeleteAsync(incomeId, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _cache
            .Setup(x => x.InvalidateUserAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.DeleteAsync(incomeId, TestUsers.PrimaryUserId, ct);

        result.IsSuccess.Should().BeTrue();
        _cache.Verify(x => x.InvalidateUserAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-INCOME-GETBYID-001 returns not found when the income does not belong to the user")]
    public async Task GetByIdAsync_ShouldFail_WhenIncomeIsMissing()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var incomeId = Guid.NewGuid();

        _repository
            .Setup(x => x.GetByIdAsync(incomeId, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vela.Domain.Entities.Income?)null);

        var result = await service.GetByIdAsync(incomeId, TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Income record not found.");
    }

    [Fact(DisplayName = "UT-INCOME-UPDATE-001 updates an existing income and invalidates dashboard cache")]
    public async Task UpdateAsync_ShouldUpdateIncomeAndInvalidateCache_WhenInputIsValid()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var userId = TestUsers.PrimaryUserId;
        var income = new Vela.Domain.Entities.Income(userId, 1000m, "Salary", "Old description", TestDates.OneWeekAgo);

        _repository
            .Setup(x => x.GetByIdAsync(income.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(income);

        _repository
            .Setup(x => x.UpdateAsync(income, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cache
            .Setup(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.UpdateAsync(
            new UpdateIncomeInput(income.Id, 1500m, " Consulting ", "Updated description", TestDates.OneDayAgo),
            userId,
            ct);

        result.IsSuccess.Should().BeTrue();
        income.Amount.Should().Be(1500m);
        income.Source.Should().Be("Consulting");
        income.Description.Should().Be("Updated description");
        income.Date.Should().Be(TestDates.OneDayAgo);
        _cache.Verify(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private IncomeService CreateService() => new(_repository.Object, _cache.Object);
}
