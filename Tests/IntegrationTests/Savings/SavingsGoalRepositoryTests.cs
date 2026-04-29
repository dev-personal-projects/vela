using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Repositories;
using IntegrationTests.Common;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Savings;

[Collection("Postgres")]
public sealed class SavingsGoalRepositoryTests(PostgresDatabaseFixture fixture) : RepositoryIntegrationTestBase(fixture)
{
    [IntegrationFact(DisplayName = "IT-SAVINGS-REPO-001 update persists progress changes")]
    public async Task UpdateAsync_ShouldPersistProgressChanges()
    {
        Guid goalId;

        await using (var seedDb = CreateDbContext())
        {
            var goal = new SavingsGoal(TestUsers.PrimaryUserId, "Emergency Fund", 1000m, TestDates.NextWeek);
            seedDb.SavingsGoals.Add(goal);
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            goalId = goal.Id;
        }

        await using (var db = CreateDbContext())
        {
            var repository = new SavingsGoalRepository(db);
            var goal = await repository.GetByIdAsync(goalId, TestUsers.PrimaryUserId, TestContext.Current.CancellationToken);
            goal.Should().NotBeNull();
            goal!.AddContribution(250m);
            await repository.UpdateAsync(goal, TestContext.Current.CancellationToken);
        }

        await using var assertDb = CreateDbContext();
        var updatedGoal = await assertDb.SavingsGoals
            .AsNoTracking()
            .SingleAsync(x => x.Id == goalId, TestContext.Current.CancellationToken);

        updatedGoal.CurrentAmount.Should().Be(250m);
    }
}
