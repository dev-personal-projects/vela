using Vela.Domain.Enums;
using Vela.Infrastructure.Repositories;
using IntegrationTests.Common;
using LoanEntity = Vela.Domain.Entities.Loan;
using LoanRepaymentEntity = Vela.Domain.Entities.LoanRepayment;

namespace IntegrationTests.Loan;

[Collection("Postgres")]
public sealed class LoanRepaymentRepositoryTests(PostgresDatabaseFixture fixture) : RepositoryIntegrationTestBase(fixture)
{
    [IntegrationFact(DisplayName = "IT-LOAN-REPAY-REPO-001 get by loan id returns only matching repayments ordered by date descending")]
    public async Task GetByLoanIdAsync_ShouldReturnOnlyMatchingRepaymentsOrderedByDateDescending()
    {
        Guid loanId;

        await using (var seedDb = CreateDbContext())
        {
            var loan = new LoanEntity(TestUsers.PrimaryUserId, "John", null, 1000m, LoanType.Owed, "Loan", TestDates.OneWeekAgo, TestDates.NextWeek);
            var secondLoan = new LoanEntity(TestUsers.PrimaryUserId, "Jane", null, 800m, LoanType.Given, "Another loan", TestDates.OneWeekAgo, TestDates.NextWeek);
            seedDb.Loans.AddRange(loan, secondLoan);
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);

            seedDb.LoanRepayments.AddRange(
                new LoanRepaymentEntity(loan.Id, 100m, TestDates.ThreeDaysAgo, "Older"),
                new LoanRepaymentEntity(loan.Id, 150m, TestDates.OneDayAgo, "Latest"),
                new LoanRepaymentEntity(secondLoan.Id, 200m, TestDates.UtcNow, "Other loan"));
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            loanId = loan.Id;
        }

        await using var db = CreateDbContext();
        var repository = new LoanRepaymentRepository(db);

        var repayments = await repository.GetByLoanIdAsync(loanId, TestContext.Current.CancellationToken);

        repayments.Should().HaveCount(2);
        repayments.Select(x => x.Note).Should().ContainInOrder("Latest", "Older");
    }
}
