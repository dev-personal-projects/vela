using Vela.Application.Interfaces;
using Vela.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Vela.Api.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IIncomeRepository, IncomeRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ISavingsGoalRepository, SavingsGoalRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<ILoanRepaymentRepository, LoanRepaymentRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        return services;
    }
}
