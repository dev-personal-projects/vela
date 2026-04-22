using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Api.GraphQL.Payloads;

public sealed record SignUpPayloadResult(string? Message, IReadOnlyList<string>? Errors);

public sealed record SignInPayloadResult(SignInPayload? SignIn, IReadOnlyList<string>? Errors);

public sealed record IncomePayload(Income? Income, IReadOnlyList<string>? Errors);

public sealed record ExpensePayload(Expense? Expense, IReadOnlyList<string>? Errors);

public sealed record SavingsGoalPayload(SavingsGoal? SavingsGoal, IReadOnlyList<string>? Errors);

public sealed record LoanPayload(Loan? Loan, IReadOnlyList<string>? Errors);

public sealed record DashboardSummary(
    decimal TotalIncome,
    decimal TotalExpenses,
    decimal NetBalance,
    IReadOnlyList<SavingsGoalProgress> SavingsProgress,
    LoanSummary LoanSummary);

public sealed record SavingsGoalProgress(
    Guid Id,
    string Name,
    decimal TargetAmount,
    decimal CurrentAmount,
    decimal Progress);

public sealed record LoanSummary(decimal TotalOwed, decimal TotalGiven);
