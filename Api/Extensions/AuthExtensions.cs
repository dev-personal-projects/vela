using FinanceTracker.Application.Interfaces;
using FinanceTracker.Api.Functions.Support;
using FinanceTracker.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceTracker.Api.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddApiAuth(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IApiTokenValidator, ApiTokenValidator>();
        services.AddSingleton<FunctionAuth>();
        return services;
    }
}
