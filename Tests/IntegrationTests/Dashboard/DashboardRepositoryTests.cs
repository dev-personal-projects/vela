using Vela.Application.DTOs.Dashboard;
using Vela.Domain.Enums;
using Vela.Infrastructure.Repositories;
using IntegrationTests.Common;
using ExpenseEntity = Vela.Domain.Entities.Expense;
using IncomeEntity = Vela.Domain.Entities.Income;
using LoanEntity = Vela.Domain.Entities.Loan;
using LoanRepaymentEntity = Vela.Domain.Entities.LoanRepayment;
using SavingsGoalEntity = Vela.Domain.Entities.SavingsGoal;

namespace IntegrationTests.Dashboard;

[Collection("Postgres")]
public sealed class DashboardRepositoryTests(PostgresDatabaseFixture fixture) : RepositoryIntegrationTestBase(fixture)
{
    [IntegrationFact(DisplayName = "IT-DASH-REPO-001 total income and expenses honor the requested date range")]
    public async Task Totals_ShouldHonorDateRangeFilters()
    {
        await using (var seedDb = CreateDbContext())
        {
            seedDb.Incomes.AddRange(
                new IncomeEntity(TestUsers.PrimaryUserId, 100m, "Salary", "Included", TestDates.OneDayAgo),
                new IncomeEntity(TestUsers.PrimaryUserId, 200m, "Bonus", "Excluded", TestDates.OneWeekAgo));
            seedDb.Expenses.AddRange(
                new ExpenseEntity(TestUsers.PrimaryUserId, 40m, "Food", "Included", TestDates.OneDayAgo),
                new ExpenseEntity(TestUsers.PrimaryUserId, 80m, "Travel", "Excluded", TestDates.OneWeekAgo));
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = CreateDbContext();
        var repository = new DashboardRepository(db);

        var totalIncome = await repository.GetTotalIncomeAsync(
            TestUsers.PrimaryUserId,
            TestDates.ThreeDaysAgo,
            TestDates.UtcNow,
            TestContext.Current.CancellationToken);

        var totalExpenses = await repository.GetTotalExpensesAsync(
            TestUsers.PrimaryUserId,
            TestDates.ThreeDaysAgo,
            TestDates.UtcNow,
            TestContext.Current.CancellationToken);

        totalIncome.Should().Be(100m);
        totalExpenses.Should().Be(40m);
    }

    [IntegrationFact(DisplayName = "IT-DASH-REPO-002 loan summary computes owed given active overdue and settled counts")]
    public async Task GetLoanSummaryAsync_ShouldComputeSnapshotAcrossLoanStates()
    {
        await using (var seedDb = CreateDbContext())
        {
            var activeLoan = new LoanEntity(TestUsers.PrimaryUserId, "Alice", null, 500m, LoanType.Owed, "Active", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(5));
            var overdueLoan = new LoanEntity(TestUsers.PrimaryUserId, "Bob", null, 300m, LoanType.Owed, "Overdue", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-1));
            var settledLoan = new LoanEntity(TestUsers.PrimaryUserId, "Charlie", null, 400m, LoanType.Given, "Settled", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(3));
            settledLoan.Repay(400m);
            var otherUserLoan = new LoanEntity(TestUsers.SecondaryUserId, "Other", null, 999m, LoanType.Owed, "Ignore", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(4));

            seedDb.Loans.AddRange(activeLoan, overdueLoan, settledLoan, otherUserLoan);
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = CreateDbContext();
        var repository = new DashboardRepository(db);

        var summary = await repository.GetLoanSummaryAsync(TestUsers.PrimaryUserId, TestContext.Current.CancellationToken);

        summary.Should().BeEquivalentTo(new DashboardLoanSummarySnapshot(
            TotalOwed: 800m,
            TotalGiven: 0m,
            ActiveLoanCount: 1,
            OverdueLoanCount: 1,
            SettledLoanCount: 1));
    }

    [IntegrationFact(DisplayName = "IT-DASH-REPO-003 recent activity merges sources and applies the overall limit")]
    public async Task GetRecentActivityAsync_ShouldMergeActivityAndApplyOverallLimit()
    {
        await using (var seedDb = CreateDbContext())
        {
            var loan = new LoanEntity(TestUsers.PrimaryUserId, "John Smith", null, 1000m, LoanType.Owed, "Loan", TestDates.OneWeekAgo, TestDates.NextWeek);
            seedDb.Loans.Add(loan);
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);

            seedDb.Incomes.Add(new IncomeEntity(TestUsers.PrimaryUserId, 500m, "Salary", "Income entry", TestDates.ThreeDaysAgo));
            seedDb.Expenses.Add(new ExpenseEntity(TestUsers.PrimaryUserId, 50m, "Food", "Expense entry", TestDates.OneDayAgo));
            seedDb.SavingsGoals.Add(new SavingsGoalEntity(TestUsers.PrimaryUserId, "Emergency Fund", 1000m, TestDates.NextWeek));
            seedDb.LoanRepayments.Add(new LoanRepaymentEntity(loan.Id, 100m, TestDates.UtcNow, "Repayment entry"));
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = CreateDbContext();
        var repository = new DashboardRepository(db);

        var activity = await repository.GetRecentActivityAsync(
            TestUsers.PrimaryUserId,
            null,
            null,
            3,
            TestContext.Current.CancellationToken);

        activity.Should().HaveCount(3);
        activity.Should().Contain(x => x.Type == DashboardActivityType.LoanRepayment && x.Title == "John Smith");
        activity.Should().Contain(x => x.Type == DashboardActivityType.SavingsGoal && x.Title == "Emergency Fund");
        activity.MaxBy(x => x.HappenedAt)!.Type.Should().Be(DashboardActivityType.SavingsGoal);
    }
}
