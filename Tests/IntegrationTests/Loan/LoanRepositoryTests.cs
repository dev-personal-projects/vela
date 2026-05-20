using Vela.Domain.Enums;
using Vela.Infrastructure.Repositories;
using IntegrationTests.Common;
using LoanEntity = Vela.Domain.Entities.Loan;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Loan;

[Collection("Postgres")]
public sealed class LoanRepositoryTests(PostgresDatabaseFixture fixture) : RepositoryIntegrationTestBase(fixture)
{
    [IntegrationFact(DisplayName = "IT-LOAN-REPO-001 get all filters by loan type and user")]
    public async Task GetAllAsync_ShouldFilterByTypeAndUser()
    {
        await using (var seedDb = CreateDbContext())
        {
            seedDb.Loans.AddRange(
                new LoanEntity(TestUsers.PrimaryUserId, "Alice", null, 100m, LoanType.Owed, "Owed", TestDates.OneDayAgo, TestDates.NextWeek),
                new LoanEntity(TestUsers.PrimaryUserId, "Bob", null, 200m, LoanType.Given, "Given", TestDates.ThreeDaysAgo, TestDates.NextWeek),
                new LoanEntity(TestUsers.SecondaryUserId, "Charlie", null, 300m, LoanType.Owed, "Other user", TestDates.UtcNow, TestDates.NextWeek));
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var db = CreateDbContext();
        var repository = new LoanRepository(db);

        var owedLoans = await repository.GetAllAsync(TestUsers.PrimaryUserId, LoanType.Owed, TestContext.Current.CancellationToken);

        owedLoans.Should().HaveCount(1);
        owedLoans[0].CounterpartyName.Should().Be("Alice");
        owedLoans[0].Type.Should().Be(LoanType.Owed);
    }

    [IntegrationFact(DisplayName = "IT-LOAN-REPO-002 update persists changed loan values")]
    public async Task UpdateAsync_ShouldPersistUpdatedValues()
    {
        Guid loanId;

        await using (var seedDb = CreateDbContext())
        {
            var loan = new LoanEntity(TestUsers.PrimaryUserId, "John", null, 500m, LoanType.Owed, "Old", TestDates.OneWeekAgo, TestDates.NextWeek);
            seedDb.Loans.Add(loan);
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            loanId = loan.Id;
        }

        await using (var db = CreateDbContext())
        {
            var repository = new LoanRepository(db);
            var loan = await repository.GetByIdAsync(loanId, TestUsers.PrimaryUserId, TestContext.Current.CancellationToken);
            loan.Should().NotBeNull();
            loan!.Update("Jane", "12345678", 650m, "Updated", TestDates.NextWeek.AddDays(3));
            await repository.UpdateAsync(loan, TestContext.Current.CancellationToken);
        }

        await using var assertDb = CreateDbContext();
        var updatedLoan = await assertDb.Loans
            .AsNoTracking()
            .SingleAsync(x => x.Id == loanId, TestContext.Current.CancellationToken);

        updatedLoan.CounterpartyName.Should().Be("Jane");
        updatedLoan.CounterpartyPhoneNumber.Should().Be("12345678");
        updatedLoan.Amount.Should().Be(650m);
        updatedLoan.Description.Should().Be("Updated");
    }

    [IntegrationFact(DisplayName = "IT-LOAN-REPO-003 delete respects user scoping")]
    public async Task DeleteAsync_ShouldRespectUserScoping()
    {
        Guid loanId;

        await using (var seedDb = CreateDbContext())
        {
            var loan = new LoanEntity(TestUsers.SecondaryUserId, "Other User Loan", null, 300m, LoanType.Given, "Other", TestDates.OneDayAgo, TestDates.NextWeek);
            seedDb.Loans.Add(loan);
            await seedDb.SaveChangesAsync(TestContext.Current.CancellationToken);
            loanId = loan.Id;
        }

        await using var db = CreateDbContext();
        var repository = new LoanRepository(db);

        var deleted = await repository.DeleteAsync(loanId, TestUsers.PrimaryUserId, TestContext.Current.CancellationToken);

        deleted.Should().BeFalse();
    }
}
