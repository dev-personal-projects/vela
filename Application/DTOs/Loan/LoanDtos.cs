using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Application.DTOs.Loan;

public sealed record AddLoanInput(
    string CounterpartyName,
    decimal Amount,
    LoanType Type,
    string Description,
    DateTime Date,
    DateTime? DueDate);

public sealed record RecordRepaymentInput(
    Guid LoanId,
    decimal Amount);
