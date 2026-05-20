using Vela.Application.Interfaces;
using Vela.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Vela.Api.Extensions;

public static class CachingExtensions
{
    public static IServiceCollection AddDashboardCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DashboardCacheOptions>(configuration.GetSection(DashboardCacheOptions.SectionName));

        var redisConnectionString = configuration["Redis:ConnectionString"];
        var redisInstanceName = configuration["Redis:InstanceName"];

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = string.IsNullOrWhiteSpace(redisInstanceName)
                    ? "Vela:"
                    : redisInstanceName;
            });
        }

        services.AddSingleton<IDashboardCacheService, DashboardCacheService>();
        services.AddSingleton<IIdempotencyService, IdempotencyService>();

        return services;
    }
}
