using Vela.Infrastructure.Repositories;
using IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;
using ExpenseEntity = Vela.Domain.Entities.Expense;

namespace IntegrationTests.Expense;

[Collection("Postgres")]
public sealed class ExpenseRepositoryTests(PostgresDatabaseFixture fixture) : RepositoryIntegrationTestBase(fixture)
{
    [IntegrationFact(DisplayName = "IT-EXPENSE-REPO-001 update persists changed values")]
    public async Task UpdateAsync_ShouldPersistUpdatedValues()
    {
        Guid expenseId;

        await using (var seedDb = CreateDbContext())
        {
            var expense = new ExpenseEntity(TestUsers.PrimaryUserId, 50m, "Groceries", "Old description", TestDates.ThreeDaysAgo);
            seedDb.Expenses.Add(expense);
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            expenseId = expense.Id;
        }

        await using (var db = CreateDbContext())
        {
            var repository = new ExpenseRepository(db);
            var expense = await repository.GetByIdAsync(expenseId, TestUsers.PrimaryUserId, TestContext.Current.CancellationToken);
            expense.Should().NotBeNull();
            expense!.Update(75m, "Transport", "Updated description", TestDates.OneDayAgo);

            await repository.UpdateAsync(expense, TestContext.Current.CancellationToken);
        }

        await using var assertDb = CreateDbContext();
        var updatedExpense = await assertDb.Expenses
            .AsNoTracking()
            .SingleAsync(x => x.Id == expenseId, TestContext.Current.CancellationToken);

        updatedExpense.Amount.Should().Be(75m);
        updatedExpense.Category.Should().Be("Transport");
        updatedExpense.Description.Should().Be("Updated description");
        updatedExpense.Date.Should().Be(TestDates.OneDayAgo);
    }

    [IntegrationFact(DisplayName = "IT-EXPENSE-REPO-002 get all returns only the user's expenses ordered by date descending")]
    public async Task GetAllAsync_ShouldFilterByUserAndOrderByDateDescending()
    {
        await using (var seedDb = CreateDbContext())
        {
            seedDb.Expenses.AddRange(
                new ExpenseEntity(TestUsers.PrimaryUserId, 80m, "Groceries", "Latest", TestDates.OneDayAgo),
                new ExpenseEntity(TestUsers.PrimaryUserId, 20m, "Transport", "Older", TestDates.OneWeekAgo),
                new ExpenseEntity(TestUsers.SecondaryUserId, 40m, "Other", "Other user", TestDates.UtcNow));
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = CreateDbContext();
        var repository = new ExpenseRepository(db);

        var expenses = await repository.GetAllAsync(TestUsers.PrimaryUserId, TestContext.Current.CancellationToken);

        expenses.Should().HaveCount(2);
        expenses.Select(x => x.Category).Should().ContainInOrder("Groceries", "Transport");
    }
}
