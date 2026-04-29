using FinanceTracker.Application.DTOs.Auth;
using FinanceTracker.Domain.Entities;

namespace FinanceTracker.Api.GraphQL.Payloads;

public sealed record SignUpPayloadResult(string? Message, IReadOnlyList<string>? Errors);

public sealed record SignInPayloadResult(SignInPayload? SignIn, IReadOnlyList<string>? Errors);

public sealed record IncomePayload(Income? Income, IReadOnlyList<string>? Errors, string? Message);

public sealed record ExpensePayload(Expense? Expense, IReadOnlyList<string>? Errors, string? Message);

public sealed record SavingsGoalPayload(SavingsGoal? SavingsGoal, IReadOnlyList<string>? Errors, string? Message);

public sealed record LoanPayload(Loan? Loan, IReadOnlyList<string>? Errors, string? Message);

public sealed record DeletePayload(bool Success, IReadOnlyList<string>? Errors, string? Message);
