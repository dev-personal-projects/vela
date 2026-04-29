using FinanceTracker.Application.DTOs.Expense;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Application.Services;
using UnitTests.Common;

namespace UnitTests.Expense;

public sealed class ExpenseServiceTests
{
    private readonly Mock<IExpenseRepository> _repository = new();
    private readonly Mock<IDashboardCacheService> _cache = new();

    [Fact(DisplayName = "UT-EXPENSE-ADD-001 stores valid expense and invalidates dashboard cache")]
    public async Task AddAsync_ShouldPersistExpenseAndInvalidateCache_WhenInputIsValid()
    {
        var service = CreateService();
        var userId = TestUsers.PrimaryUserId;
        var ct = TestContext.Current.CancellationToken;
        FinanceTracker.Domain.Entities.Expense? savedExpense = null;

        _repository
            .Setup(x => x.AddAsync(It.IsAny<FinanceTracker.Domain.Entities.Expense>(), It.IsAny<CancellationToken>()))
            .Callback<FinanceTracker.Domain.Entities.Expense, CancellationToken>((expense, _) => savedExpense = expense)
            .Returns(Task.CompletedTask);

        _cache
            .Setup(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.AddAsync(new AddExpenseInput(150m, " Groceries ", "Weekly shop", TestDates.OneDayAgo), userId, ct);

        result.IsSuccess.Should().BeTrue();
        savedExpense.Should().NotBeNull();
        savedExpense!.Category.Should().Be("Groceries");
        savedExpense.Amount.Should().Be(150m);
        _cache.Verify(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-EXPENSE-ADD-002 rejects empty categories before persistence")]
    public async Task AddAsync_ShouldFail_WhenCategoryIsMissing()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.AddAsync(new AddExpenseInput(150m, "   ", "Weekly shop", TestDates.OneDayAgo), TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Category is required.");

        _repository.Verify(x => x.AddAsync(It.IsAny<FinanceTracker.Domain.Entities.Expense>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-EXPENSE-GETBYID-001 returns not found when the expense does not exist")]
    public async Task GetByIdAsync_ShouldFail_WhenExpenseIsMissing()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var expenseId = Guid.NewGuid();

        _repository
            .Setup(x => x.GetByIdAsync(expenseId, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FinanceTracker.Domain.Entities.Expense?)null);

        var result = await service.GetByIdAsync(expenseId, TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Expense record not found.");
    }

    [Fact(DisplayName = "UT-EXPENSE-UPDATE-001 updates an existing expense and invalidates dashboard cache")]
    public async Task UpdateAsync_ShouldUpdateExpenseAndInvalidateCache_WhenInputIsValid()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var userId = TestUsers.PrimaryUserId;
        var expense = new FinanceTracker.Domain.Entities.Expense(userId, 50m, "Groceries", "Old shop", TestDates.OneWeekAgo);

        _repository
            .Setup(x => x.GetByIdAsync(expense.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);

        _repository
            .Setup(x => x.UpdateAsync(expense, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _cache
            .Setup(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.UpdateAsync(
            new UpdateExpenseInput(expense.Id, 75m, " Transport ", "Bus fare", TestDates.OneDayAgo),
            userId,
            ct);

        result.IsSuccess.Should().BeTrue();
        expense.Amount.Should().Be(75m);
        expense.Category.Should().Be("Transport");
        expense.Description.Should().Be("Bus fare");
        _cache.Verify(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-EXPENSE-DELETE-001 invalidates dashboard cache after a successful delete")]
    public async Task DeleteAsync_ShouldInvalidateCache_WhenDeleteSucceeds()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var expenseId = Guid.NewGuid();

        _repository
            .Setup(x => x.DeleteAsync(expenseId, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _cache
            .Setup(x => x.InvalidateUserAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.DeleteAsync(expenseId, TestUsers.PrimaryUserId, ct);

        result.IsSuccess.Should().BeTrue();
        _cache.Verify(x => x.InvalidateUserAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private ExpenseService CreateService() => new(_repository.Object, _cache.Object);
}
