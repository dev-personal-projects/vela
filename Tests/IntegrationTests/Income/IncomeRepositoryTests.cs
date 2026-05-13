using FinanceTracker.Infrastructure.Repositories;
using IntegrationTests.Common;
using IncomeEntity = FinanceTracker.Domain.Entities.Income;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Income;

[Collection("Postgres")]
public sealed class IncomeRepositoryTests(PostgresDatabaseFixture fixture) : RepositoryIntegrationTestBase(fixture)
{
    [IntegrationFact(DisplayName = "IT-INCOME-REPO-001 returns only the user's incomes ordered by date descending")]
    public async Task GetAllAsync_ShouldFilterByUserAndOrderByDateDescending()
    {
        await using (var seedDb = CreateDbContext())
        {
            seedDb.Incomes.AddRange(
                new IncomeEntity(TestUsers.PrimaryUserId, 5000m, "Salary", "Latest", TestDates.OneDayAgo),
                new IncomeEntity(TestUsers.PrimaryUserId, 2500m, "Freelance", "Older", TestDates.ThreeDaysAgo),
                new IncomeEntity(TestUsers.SecondaryUserId, 900m, "Other", "Other user", TestDates.UtcNow));
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = CreateDbContext();
        var repository = new IncomeRepository(db);

        var incomes = await repository.GetAllAsync(TestUsers.PrimaryUserId, TestContext.Current.CancellationToken);

        incomes.Should().HaveCount(2);
        incomes.Select(x => x.Source).Should().ContainInOrder("Salary", "Freelance");
        incomes.Should().OnlyContain(x => x.UserId == TestUsers.PrimaryUserId);
    }

    [IntegrationFact(DisplayName = "IT-INCOME-REPO-002 get by id returns null for another user's income")]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIncomeBelongsToAnotherUser()
    {
        Guid incomeId;

        await using (var seedDb = CreateDbContext())
        {
            var income = new IncomeEntity(TestUsers.SecondaryUserId, 1000m, "Gift", "Other user", TestDates.OneDayAgo);
            seedDb.Incomes.Add(income);
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            incomeId = income.Id;
        }

        await using var db = CreateDbContext();
        var repository = new IncomeRepository(db);

        var incomeForPrimaryUser = await repository.GetByIdAsync(incomeId, TestUsers.PrimaryUserId, TestContext.Current.CancellationToken);

        incomeForPrimaryUser.Should().BeNull();
    }

    [IntegrationFact(DisplayName = "IT-INCOME-REPO-003 delete removes only the matching user's record")]
    public async Task DeleteAsync_ShouldDeleteOnlyMatchingUserRecord()
    {
        Guid primaryIncomeId;
        Guid secondaryIncomeId;

        await using (var seedDb = CreateDbContext())
        {
            var primaryIncome = new IncomeEntity(TestUsers.PrimaryUserId, 100m, "Salary", "Primary", TestDates.OneDayAgo);
            var secondaryIncome = new IncomeEntity(TestUsers.SecondaryUserId, 200m, "Gift", "Secondary", TestDates.OneDayAgo);
            seedDb.Incomes.AddRange(primaryIncome, secondaryIncome);
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            primaryIncomeId = primaryIncome.Id;
            secondaryIncomeId = secondaryIncome.Id;
        }

        await using (var db = CreateDbContext())
        {
            var repository = new IncomeRepository(db);
            var deleted = await repository.DeleteAsync(primaryIncomeId, TestUsers.PrimaryUserId, TestContext.Current.CancellationToken);
            deleted.Should().BeTrue();
        }

        await using var assertDb = CreateDbContext();
        var remainingIds = await assertDb.Incomes
            .Select(x => x.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        remainingIds.Should().NotContain(primaryIncomeId);
        remainingIds.Should().Contain(secondaryIncomeId);
    }
}
