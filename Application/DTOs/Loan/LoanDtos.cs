using FinanceTracker.Domain.Enums;

namespace FinanceTracker.Application.DTOs.Loan;

public sealed record AddLoanInput(
    string CounterpartyName,
    string? CounterpartyPhoneNumber,
    decimal Amount,
    LoanType Type,
    string Description,
    DateTime Date,
    DateTime? DueDate);

public sealed record UpdateLoanInput(
    Guid Id,
    string CounterpartyName,
    string? CounterpartyPhoneNumber,
    decimal Amount,
    string Description,
    DateTime? DueDate);

public sealed record RecordRepaymentInput(
    Guid LoanId,
    decimal Amount,
    DateTime? Date = null,
    string? Note = null);

public sealed record SettleLoanInput(
    Guid LoanId,
    DateTime? Date = null,
    string? Note = null);
