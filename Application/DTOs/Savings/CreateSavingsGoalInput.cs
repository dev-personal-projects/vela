namespace Vela.Application.DTOs.Savings;

public sealed record CreateSavingsGoalInput(
    string Name,
    decimal TargetAmount,
    DateTime? Deadline);

public sealed record UpdateSavingsGoalInput(
    Guid Id,
    string Name,
    decimal TargetAmount,
    DateTime? Deadline);

public sealed record AddSavingsContributionInput(
    Guid Id,
    decimal Amount);
