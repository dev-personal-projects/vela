using FinanceTracker.Application.Interfaces;
using FinanceTracker.Application.Services;
using FinanceTracker.Infrastructure.Auth;

namespace FinanceTracker.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IApiTokenService, ApiTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IIncomeService, IncomeService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ISavingsGoalService, SavingsGoalService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
