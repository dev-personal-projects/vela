using Vela.Application.DTOs.Loan;
using Vela.Application.Interfaces;
using Vela.Application.Services;
using Vela.Domain.Entities;
using Vela.Domain.Enums;
using UnitTests.Builders;
using UnitTests.Common;

namespace UnitTests.Loan;

public sealed class LoanServiceTests
{
    private readonly Mock<ILoanRepository> _repository = new();
    private readonly Mock<ILoanRepaymentRepository> _repaymentRepository = new();
    private readonly Mock<IDashboardCacheService> _cache = new();

    [Fact(DisplayName = "UT-LOAN-ADD-002 stores valid loans with phone numbers and invalidates dashboard cache")]
    public async Task AddAsync_ShouldPersistLoanAndInvalidateCache_WhenInputIsValid()
    {
        var service = CreateService();
        var userId = TestUsers.PrimaryUserId;
        var ct = TestContext.Current.CancellationToken;
        Vela.Domain.Entities.Loan? savedLoan = null;

        _repository
            .Setup(x => x.AddAsync(It.IsAny<Vela.Domain.Entities.Loan>(), It.IsAny<CancellationToken>()))
            .Callback<Vela.Domain.Entities.Loan, CancellationToken>((loan, _) => savedLoan = loan)
            .Returns(Task.CompletedTask);

        _cache
            .Setup(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.AddAsync(
            new AddLoanInput(" Jane Smith ", " 555-1234 ", 500m, LoanType.Given, "Family loan", TestDates.OneDayAgo, TestDates.NextMonth),
            userId,
            ct);

        result.IsSuccess.Should().BeTrue();
        savedLoan.Should().NotBeNull();
        savedLoan!.CounterpartyName.Should().Be("Jane Smith");
        savedLoan.CounterpartyPhoneNumber.Should().Be("555-1234");
        savedLoan.Type.Should().Be(LoanType.Given);
        _cache.Verify(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-LOAN-ADD-003 rejects empty counterparties")]
    public async Task AddAsync_ShouldFail_WhenCounterpartyNameIsMissing()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.AddAsync(
            new AddLoanInput("   ", null, 500m, LoanType.Owed, "Bad loan", TestDates.OneDayAgo, TestDates.NextMonth),
            TestUsers.PrimaryUserId,
            ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Counterparty name is required.");
        _repository.Verify(x => x.AddAsync(It.IsAny<Vela.Domain.Entities.Loan>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-LOAN-ADD-004 rejects non-positive loan amounts")]
    public async Task AddAsync_ShouldFail_WhenAmountIsNotPositive()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.AddAsync(
            new AddLoanInput("John Smith", null, 0m, LoanType.Owed, "Bad loan", TestDates.OneDayAgo, TestDates.NextMonth),
            TestUsers.PrimaryUserId,
            ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Amount must be greater than 0.");
        _repository.Verify(x => x.AddAsync(It.IsAny<Vela.Domain.Entities.Loan>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-LOAN-REPAY-001 records repayments and invalidates dashboard cache")]
    public async Task RecordRepaymentAsync_ShouldStoreRepaymentAndInvalidateCache_WhenInputIsValid()
    {
        var service = CreateService();
        var userId = TestUsers.PrimaryUserId;
        var ct = TestContext.Current.CancellationToken;
        var loan = new LoanBuilder()
            .WithAmount(1000m)
            .Build(userId);

        LoanRepayment? savedRepayment = null;

        _repository
            .Setup(x => x.GetByIdAsync(loan.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        _repaymentRepository
            .Setup(x => x.AddAsync(It.IsAny<LoanRepayment>(), It.IsAny<CancellationToken>()))
            .Callback<LoanRepayment, CancellationToken>((repayment, _) => savedRepayment = repayment)
            .Returns(Task.CompletedTask);

        _cache
            .Setup(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.RecordRepaymentAsync(
            new RecordRepaymentInput(loan.Id, 250m, TestDates.OneDayAgo, "First repayment"),
            userId,
            ct);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.AmountRepaid.Should().Be(250m);
        savedRepayment.Should().NotBeNull();
        savedRepayment!.Amount.Should().Be(250m);
        savedRepayment.Note.Should().Be("First repayment");
        _cache.Verify(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-LOAN-REPAY-002 rejects repayments larger than the remaining balance")]
    public async Task RecordRepaymentAsync_ShouldFail_WhenRepaymentExceedsRemainingBalance()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var loan = new LoanBuilder()
            .WithAmount(500m)
            .Build(TestUsers.PrimaryUserId);

        _repository
            .Setup(x => x.GetByIdAsync(loan.Id, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        var result = await service.RecordRepaymentAsync(
            new RecordRepaymentInput(loan.Id, 600m, TestDates.OneDayAgo, "Too much"),
            TestUsers.PrimaryUserId,
            ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be($"Repayment amount exceeds the remaining balance of {loan.RemainingBalance:C}.");

        _repaymentRepository.Verify(x => x.AddAsync(It.IsAny<LoanRepayment>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-LOAN-REPAY-003 rejects non-positive repayments")]
    public async Task RecordRepaymentAsync_ShouldFail_WhenAmountIsNotPositive()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var loan = new LoanBuilder().Build(TestUsers.PrimaryUserId);

        _repository
            .Setup(x => x.GetByIdAsync(loan.Id, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        var result = await service.RecordRepaymentAsync(
            new RecordRepaymentInput(loan.Id, 0m, TestDates.OneDayAgo, "Invalid"),
            TestUsers.PrimaryUserId,
            ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Repayment amount must be greater than 0.");

        _repaymentRepository.Verify(x => x.AddAsync(It.IsAny<LoanRepayment>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-LOAN-REPAY-004 rejects repayment dates before the loan date")]
    public async Task RecordRepaymentAsync_ShouldFail_WhenRepaymentDateIsBeforeLoanDate()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var loan = new LoanBuilder()
            .WithDate(TestDates.OneDayAgo)
            .Build(TestUsers.PrimaryUserId);

        _repository
            .Setup(x => x.GetByIdAsync(loan.Id, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        var result = await service.RecordRepaymentAsync(
            new RecordRepaymentInput(loan.Id, 100m, TestDates.OneWeekAgo, "Too early"),
            TestUsers.PrimaryUserId,
            ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Repayment date cannot be before the loan date.");

        _repaymentRepository.Verify(x => x.AddAsync(It.IsAny<LoanRepayment>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-LOAN-SETTLE-001 rejects settling loans that are already settled")]
    public async Task SettleAsync_ShouldFail_WhenLoanIsAlreadySettled()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var loan = new LoanBuilder()
            .WithAmount(500m)
            .WithAmountRepaid(500m)
            .Build(TestUsers.PrimaryUserId);

        _repository
            .Setup(x => x.GetByIdAsync(loan.Id, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        var result = await service.SettleAsync(new SettleLoanInput(loan.Id, TestDates.OneDayAgo, "Settled"), TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Loan is already settled.");

        _repaymentRepository.Verify(x => x.AddAsync(It.IsAny<LoanRepayment>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-LOAN-SETTLE-002 records final repayment and invalidates dashboard cache")]
    public async Task SettleAsync_ShouldRecordFinalRepaymentAndInvalidateCache_WhenLoanIsActive()
    {
        var service = CreateService();
        var userId = TestUsers.PrimaryUserId;
        var ct = TestContext.Current.CancellationToken;
        var loan = new LoanBuilder()
            .WithAmount(500m)
            .WithAmountRepaid(125m)
            .Build(userId);
        LoanRepayment? savedRepayment = null;

        _repository
            .Setup(x => x.GetByIdAsync(loan.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        _repaymentRepository
            .Setup(x => x.AddAsync(It.IsAny<LoanRepayment>(), It.IsAny<CancellationToken>()))
            .Callback<LoanRepayment, CancellationToken>((repayment, _) => savedRepayment = repayment)
            .Returns(Task.CompletedTask);

        _cache
            .Setup(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.SettleAsync(new SettleLoanInput(loan.Id, TestDates.OneDayAgo, "Final"), userId, ct);

        result.IsSuccess.Should().BeTrue();
        loan.IsSettled.Should().BeTrue();
        savedRepayment.Should().NotBeNull();
        savedRepayment!.Amount.Should().Be(375m);
        savedRepayment.Note.Should().Be("Final");
        _cache.Verify(x => x.InvalidateUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-LOAN-ADD-001 rejects due dates that are not after the loan date")]
    public async Task AddAsync_ShouldFail_WhenDueDateIsNotAfterLoanDate()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;

        var result = await service.AddAsync(
            new AddLoanInput("John Smith", null, 500m, LoanType.Owed, "Bad due date", TestDates.OneDayAgo, TestDates.OneDayAgo),
            TestUsers.PrimaryUserId,
            ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Due date must be after the loan date.");
        _repository.Verify(x => x.AddAsync(It.IsAny<Vela.Domain.Entities.Loan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-LOAN-UPDATE-001 rejects reducing the amount below the amount already repaid")]
    public async Task UpdateAsync_ShouldFail_WhenAmountWouldDropBelowRepaidAmount()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var loan = new LoanBuilder()
            .WithAmount(1000m)
            .WithAmountRepaid(400m)
            .Build(TestUsers.PrimaryUserId);

        _repository
            .Setup(x => x.GetByIdAsync(loan.Id, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        var result = await service.UpdateAsync(
            new UpdateLoanInput(loan.Id, "John Smith", null, 300m, "Updated", TestDates.NextMonth),
            TestUsers.PrimaryUserId,
            ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be($"Amount cannot be less than the amount already repaid ({loan.AmountRepaid:C}).");
        _repository.Verify(x => x.UpdateAsync(It.IsAny<Vela.Domain.Entities.Loan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-LOAN-UPDATE-002 rejects update due dates that are not after the loan date")]
    public async Task UpdateAsync_ShouldFail_WhenDueDateIsNotAfterLoanDate()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var loan = new LoanBuilder()
            .WithDate(TestDates.OneDayAgo)
            .Build(TestUsers.PrimaryUserId);

        _repository
            .Setup(x => x.GetByIdAsync(loan.Id, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);

        var result = await service.UpdateAsync(
            new UpdateLoanInput(loan.Id, "John Smith", null, 1000m, "Updated", TestDates.OneDayAgo),
            TestUsers.PrimaryUserId,
            ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Due date must be after the loan date.");
        _repository.Verify(x => x.UpdateAsync(It.IsAny<Vela.Domain.Entities.Loan>(), It.IsAny<CancellationToken>()), Times.Never);
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-LOAN-DELETE-001 invalidates dashboard cache after a successful delete")]
    public async Task DeleteAsync_ShouldInvalidateCache_WhenDeleteSucceeds()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var loanId = Guid.NewGuid();

        _repository
            .Setup(x => x.DeleteAsync(loanId, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _cache
            .Setup(x => x.InvalidateUserAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await service.DeleteAsync(loanId, TestUsers.PrimaryUserId, ct);

        result.IsSuccess.Should().BeTrue();
        _cache.Verify(x => x.InvalidateUserAsync(TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact(DisplayName = "UT-LOAN-DELETE-002 does not invalidate cache when delete misses")]
    public async Task DeleteAsync_ShouldNotInvalidateCache_WhenLoanIsMissing()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var loanId = Guid.NewGuid();

        _repository
            .Setup(x => x.DeleteAsync(loanId, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await service.DeleteAsync(loanId, TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Loan not found.");
        _cache.Verify(x => x.InvalidateUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact(DisplayName = "UT-LOAN-HISTORY-001 returns not found when requesting repayment history for another user's loan")]
    public async Task GetRepaymentHistoryAsync_ShouldFail_WhenLoanIsMissing()
    {
        var service = CreateService();
        var ct = TestContext.Current.CancellationToken;
        var loanId = Guid.NewGuid();

        _repository
            .Setup(x => x.GetByIdAsync(loanId, TestUsers.PrimaryUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vela.Domain.Entities.Loan?)null);

        var result = await service.GetRepaymentHistoryAsync(loanId, TestUsers.PrimaryUserId, ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Loan not found.");
        _repaymentRepository.Verify(x => x.GetByLoanIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private LoanService CreateService() => new(_repository.Object, _repaymentRepository.Object, _cache.Object);
}
