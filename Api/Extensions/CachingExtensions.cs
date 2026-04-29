using FinanceTracker.Application.Interfaces;
using FinanceTracker.Infrastructure.Caching;

namespace FinanceTracker.Api.Extensions;

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
                    ? "FinanceTracker:"
                    : redisInstanceName;
            });
        }

        services.AddSingleton<IDashboardCacheService, DashboardCacheService>();

        return services;
    }
}
