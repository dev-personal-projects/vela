namespace FinanceTracker.Application.DTOs.Savings;

public sealed record CreateSavingsGoalInput(
    string Name,
    decimal TargetAmount,
    DateTime? Deadline);
