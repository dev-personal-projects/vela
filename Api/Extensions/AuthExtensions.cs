using Vela.Application.Interfaces;
using Vela.Api.Functions.Support;
using Vela.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vela.Api.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddApiAuth(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IApiTokenValidator, ApiTokenValidator>();
        services.AddSingleton<FunctionAuth>();
        return services;
    }
}
