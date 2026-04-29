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

    private ExpenseService CreateService() => new(_repository.Object, _cache.Object);
}
