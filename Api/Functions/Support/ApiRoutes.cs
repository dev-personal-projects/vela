namespace FinanceTracker.Api.Functions.Support;

public static class ApiRoutes
{
    public const string Health = "v1/health";
    public const string AuthSignUp = "v1/auth/signup";
    public const string AuthSignIn = "v1/auth/signin";
    public const string Incomes = "v1/incomes";
    public const string IncomeById = "v1/incomes/{id:guid}";
    public const string Expenses = "v1/expenses";
    public const string ExpenseById = "v1/expenses/{id:guid}";
    public const string SavingsGoals = "v1/savings-goals";
    public const string SavingsGoalById = "v1/savings-goals/{id:guid}";
    public const string SavingsGoalContributions = "v1/savings-goals/{id:guid}/contributions";
    public const string SavingsGoalProgress = "v1/savings-goals/{id:guid}/progress";
    public const string Loans = "v1/loans";
    public const string LoanById = "v1/loans/{id:guid}";
    public const string LoanRepayments = "v1/loans/{id:guid}/repayments";
    public const string LoanSettle = "v1/loans/{id:guid}/settle";
    public const string Dashboard = "v1/dashboard";
}
