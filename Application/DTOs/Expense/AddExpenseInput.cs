namespace FinanceTracker.Application.DTOs.Expense;

public sealed record AddExpenseInput(
    decimal Amount,
    string Category,
    string Description,
    DateTime Date);

public sealed record UpdateExpenseInput(
    Guid Id,
    decimal Amount,
    string Category,
    string Description,
    DateTime Date);
