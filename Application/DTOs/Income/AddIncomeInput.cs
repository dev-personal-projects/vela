namespace FinanceTracker.Application.DTOs.Income;

public sealed record AddIncomeInput(
    decimal Amount,
    string Source,
    string Description,
    DateTime Date);
